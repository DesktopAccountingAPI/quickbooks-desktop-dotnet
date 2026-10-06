using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DesktopAccountingApi.QuickBooksDesktop.Models;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>A parsed result together with the HTTP response it came from (<c>...WithResponseAsync</c> methods).</summary>
/// <typeparam name="T">The result type.</typeparam>
public sealed class ApiResponse<T>
{
    internal ApiResponse(T data, int statusCode, IReadOnlyDictionary<string, string> headers)
    {
        Data = data;
        StatusCode = statusCode;
        Headers = headers;
    }

    /// <summary>The parsed result.</summary>
    public T Data { get; }

    /// <summary>HTTP status code. After the SDK long-polled a timed-out request, this is the status of the final poll.</summary>
    public int StatusCode { get; }

    /// <summary>Response headers; names are case-insensitive. Multiple values are joined with <c>", "</c>.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>The <c>Daapi-Request-Id</c> header.</summary>
    public string? RequestId => Headers.TryGetValue("Daapi-Request-Id", out var v) ? v : null;

    /// <summary>The <c>Daapi-Warnings</c> header (number of QuickBooks warnings recorded on the request), if present.</summary>
    public string? Warnings => Headers.TryGetValue("Daapi-Warnings", out var v) ? v : null;
}

/// <summary>One page of a cursor-paginated list.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class Page<T>
{
    internal Page(IReadOnlyList<T> data, string? nextCursor, bool hasMore, int? remainingCount, DateTimeOffset? cursorExpiresAt, string? url, string? requestId, IReadOnlyList<string?> ids, IReadOnlyList<string?> updatedAts)
    {
        Data = data;
        NextCursor = nextCursor;
        HasMore = hasMore;
        RemainingCount = remainingCount;
        CursorExpiresAt = cursorExpiresAt;
        Url = url;
        RequestId = requestId;
        ItemIds = ids;
        ItemUpdatedAts = updatedAts;
    }

    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>Cursor for the next page, or <c>null</c> on the last page.</summary>
    public string? NextCursor { get; }

    /// <summary>Whether more pages follow.</summary>
    public bool HasMore { get; }

    /// <summary>Items left after this page, when the API knows it (QuickBooks lists), else <c>null</c>.</summary>
    public int? RemainingCount { get; }

    /// <summary>The server's estimate of when <see cref="NextCursor"/> expires if unused. <c>null</c> on the last page and for platform lists.</summary>
    public DateTimeOffset? CursorExpiresAt { get; }

    /// <summary>The list's URL path.</summary>
    public string? Url { get; }

    /// <summary>The <c>Daapi-Request-Id</c> of the response that returned this page.</summary>
    public string? RequestId { get; }

    internal IReadOnlyList<string?> ItemIds { get; }

    internal IReadOnlyList<string?> ItemUpdatedAts { get; }

    internal static Page<T> Parse(string json, string? requestId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new DaapiException("The list response has no data array.");
        }
        var items = new List<T>(data.GetArrayLength());
        var ids = new List<string?>(items.Capacity);
        var updatedAts = new List<string?>(items.Capacity);
        foreach (var element in data.EnumerateArray())
        {
            items.Add(element.Deserialize<T>(DesktopAccountingApiJson.Options)!);
            ids.Add(StringProp(element, "id"));
            updatedAts.Add(StringProp(element, "updatedAt"));
        }
        var hasMore = root.TryGetProperty("hasMore", out var hm) && hm.ValueKind == JsonValueKind.True;
        int? remaining = root.TryGetProperty("remainingCount", out var rc) && rc.ValueKind == JsonValueKind.Number ? rc.GetInt32() : null;
        DateTimeOffset? expires = null;
        if (StringProp(root, "cursorExpiresAt") is { } exp && DateTimeOffset.TryParse(exp, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var e)) expires = e;
        return new Page<T>(items, StringProp(root, "nextCursor"), hasMore, remaining, expires, StringProp(root, "url"), requestId, ids, updatedAts);
    }

    private static string? StringProp(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>
/// Lazily iterates a cursor-paginated list. <c>await foreach</c> yields every item across pages and
/// requests page N+1 as soon as page N arrives (one page of read-ahead) to stay inside the cursor's
/// idle window. A network error on a continue request retries the same cursor. An expired cursor
/// raises <see cref="CursorExpiredException"/> with progress fields; the pager never restarts silently.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class Pager<T> : IAsyncEnumerable<T>
{
    private readonly Func<string?, CancellationToken, Task<Page<T>>> _fetch;
    private readonly CancellationToken _token;

    internal Pager(Func<string?, CancellationToken, Task<Page<T>>> fetch, CancellationToken cancellationToken)
    {
        _fetch = fetch;
        _token = cancellationToken;
    }

    /// <summary>Fetches only the first page.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The first page.</returns>
    public async Task<Page<T>> GetFirstPageAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        return await _fetch(null, linked.Token).ConfigureAwait(false);
    }

    /// <summary>Iterates page by page (with one page of read-ahead).</summary>
    /// <param name="cancellationToken">Stops the iteration.</param>
    /// <returns>The pages in order.</returns>
    public IAsyncEnumerable<Page<T>> PagesAsync(CancellationToken cancellationToken = default) => IteratePagesAsync(new Progress(), cancellationToken);

    /// <summary>Reads every item into a list, fetching pages as fast as possible.</summary>
    /// <param name="cancellationToken">Stops the iteration.</param>
    /// <returns>All items.</returns>
    public async Task<List<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<T>();
        await foreach (var item in ItemsAsync(cancellationToken).ConfigureAwait(false)) all.Add(item);
        return all;
    }

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ItemsAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

    private async IAsyncEnumerable<T> ItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var progress = new Progress { CountItems = true };
        await foreach (var page in IteratePagesAsync(progress, cancellationToken).ConfigureAwait(false))
        {
            for (var i = 0; i < page.Data.Count; i++)
            {
                progress.ItemsYielded++;
                progress.LastId = page.ItemIds[i];
                progress.LastUpdatedAt = page.ItemUpdatedAts[i];
                yield return page.Data[i];
            }
        }
    }

    private sealed class Progress
    {
        public bool CountItems;
        public int ItemsYielded;
        public int Pages;
        public string? LastId;
        public string? LastUpdatedAt;
    }

    private async IAsyncEnumerable<Page<T>> IteratePagesAsync(Progress progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        Task<Page<T>>? next = _fetch(null, cts.Token);
        try
        {
            while (next is not null)
            {
                Page<T> page;
                try
                {
                    page = await next.ConfigureAwait(false);
                }
                catch (CursorExpiredException ex)
                {
                    next = null;
                    throw new CursorExpiredException(ex, progress.ItemsYielded, progress.Pages, progress.LastId, progress.LastUpdatedAt);
                }
                progress.Pages++;
                // Read-ahead: request the next page before handing this one to the caller.
                next = page.HasMore && page.NextCursor is { } cursor ? _fetch(cursor, cts.Token) : null;
                if (!progress.CountItems)
                {
                    progress.ItemsYielded += page.Data.Count;
                    if (page.Data.Count > 0)
                    {
                        progress.LastId = page.ItemIds[page.Data.Count - 1];
                        progress.LastUpdatedAt = page.ItemUpdatedAts[page.Data.Count - 1];
                    }
                }
                yield return page;
            }
        }
        finally
        {
            if (next is not null && !next.IsCompleted)
            {
                // The caller stopped early: cancel the read-ahead request and observe its outcome.
                cts.Cancel();
                var source = cts;
                _ = next.ContinueWith(t => { _ = t.Exception; source.Dispose(); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            else
            {
                if (next is { IsFaulted: true }) _ = next.Exception;
                cts.Dispose();
            }
        }
    }
}

/// <summary>
/// A request queued in async mode (<c>Enqueue.XxxAsync</c>). QuickBooks runs it when the Web
/// Connector next connects; use <see cref="WaitAsync"/> to long-poll for the result,
/// <see cref="ResultAsync"/> to check once, or a webhook.
/// </summary>
/// <typeparam name="T">The operation's result type.</typeparam>
public sealed class RequestHandle<T>
{
    private readonly ApiCore _core;
    private readonly Func<string, T> _parse;
    private readonly RequestOptions? _options;

    internal RequestHandle(ApiCore core, Request request, Func<string, T> parse, RequestOptions? options)
    {
        _core = core;
        Request = request;
        _parse = parse;
        _options = options;
    }

    /// <summary>The request ID (<c>req_...</c>).</summary>
    public string Id => Request.Id;

    /// <summary>The request snapshot returned by the <c>202 Accepted</c> response.</summary>
    public Request Request { get; }

    /// <summary>Fetches the current request resource (no waiting).</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The current request.</returns>
    public async Task<Request> StatusAsync(CancellationToken cancellationToken = default) =>
        (await _core.GetRequestAsync(Id, null, _options, cancellationToken).ConfigureAwait(false)).Request;

    /// <summary>
    /// Long-polls <c>GET /v1/requests/{id}?waitSeconds=N</c> until the request finishes or
    /// <paramref name="timeout"/> elapses. Returns the typed result when it succeeded; throws the
    /// typed <see cref="ApiException"/> when it failed, was canceled or became <c>outcome_unknown</c>;
    /// throws <see cref="RequestPendingException"/> when the time is up.
    /// </summary>
    /// <param name="timeout">How long to wait. Default: the client timeout.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The operation's result.</returns>
    public Task<T> WaitAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        _core.WaitForResultAsync(Id, Request, _parse, timeout, _options, cancellationToken);

    /// <summary>Checks once: returns the result if the request succeeded, throws its typed error if it failed, or throws <see cref="RequestPendingException"/> if it has not finished.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The operation's result.</returns>
    public async Task<T> ResultAsync(CancellationToken cancellationToken = default)
    {
        var current = await _core.GetRequestAsync(Id, null, _options, cancellationToken).ConfigureAwait(false);
        if (ApiCore.TryResolve(current.Request, _parse, out var result)) return result;
        throw new RequestPendingException(Id, current.Request);
    }
}
