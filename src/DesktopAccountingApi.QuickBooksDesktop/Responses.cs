using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    internal ApiResponse(T data, int statusCode, IReadOnlyDictionary<string, string> headers, string? idempotencyKey = null, string? requestId = null)
    {
        Data = data;
        StatusCode = statusCode;
        Headers = headers;
        IdempotencyKey = idempotencyKey;
        RequestId = requestId ?? (headers.TryGetValue("Daapi-Request-Id", out var v) ? v : null);
    }

    /// <summary>The <c>Idempotency-Key</c> the SDK sent for a write (generated unless you set one), else <c>null</c>.</summary>
    public string? IdempotencyKey { get; }

    /// <summary>The parsed result.</summary>
    public T Data { get; }

    /// <summary>HTTP status code. After the SDK long-polled a timed-out request, this is the status of the final poll.</summary>
    public int StatusCode { get; }

    /// <summary>Response headers; names are case-insensitive. Multiple values are joined with <c>", "</c>.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// The ID of the request that produced the result: the <c>Daapi-Request-Id</c> header, or, after the SDK
    /// long-polled a request that timed out on the server (<c>504 QBD_REQUEST_TIMEOUT</c>), that request's ID,
    /// which <c>client.Requests.RetrieveAsync</c> finds. The final poll's own ID stays in <see cref="Headers"/>.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>The number of QuickBooks warnings recorded on the request (the <c>Daapi-Warnings</c> header); 0 when the header is absent or not a number.</summary>
    public int Warnings =>
        Headers.TryGetValue("Daapi-Warnings", out var v) && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;
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
/// Lazily iterates a cursor-paginated list. <c>await foreach</c> yields every item across pages.
/// The next page is requested only when the iteration needs it, so a loop that stops early sends no
/// extra QuickBooks query. While you work through a page item by item, the next page is requested in
/// the background once the page has been in hand for 2 seconds, so slow consumers stay inside the
/// cursor's idle window (about 10 seconds). <see cref="ListAllAsync"/> always reads one page ahead.
/// A network error on a continue request retries the same cursor. An expired cursor raises
/// <see cref="CursorExpiredException"/> with progress fields; the pager never restarts silently.
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

    /// <summary>How long the item iterator holds a page before it requests the next one in the background. Tests lower it.</summary>
    internal static TimeSpan ReadAheadAfter { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Fetches only the first page.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The first page.</returns>
    public async Task<Page<T>> GetFirstPageAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        return await _fetch(null, linked.Token).ConfigureAwait(false);
    }

    /// <summary>Iterates page by page; each page is requested when you ask for it.</summary>
    /// <param name="cancellationToken">Stops the iteration.</param>
    /// <returns>The pages in order.</returns>
    public IAsyncEnumerable<Page<T>> PagesAsync(CancellationToken cancellationToken = default) => IteratePagesAsync(cancellationToken);

    /// <summary>Reads every item into a list, requesting each next page as soon as a page arrives.</summary>
    /// <param name="cancellationToken">Stops the iteration.</param>
    /// <returns>All items.</returns>
    public async Task<List<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<T>();
        var progress = new Progress();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        Task<Page<T>>? next = _fetch(null, cts.Token);
        try
        {
            while (next is not null)
            {
                var page = await AwaitPageAsync(next, progress).ConfigureAwait(false);
                // Read-ahead: request the next page before handling this one.
                next = page.HasMore && page.NextCursor is { } cursor ? _fetch(cursor, cts.Token) : null;
                progress.Record(page);
                all.AddRange(page.Data);
            }
            return all;
        }
        finally
        {
            Release(next, cts);
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ItemsAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

    private async IAsyncEnumerable<T> ItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var progress = new Progress();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        Task<Page<T>>? next = null;
        try
        {
            var page = await AwaitPageAsync(_fetch(null, cts.Token), progress).ConfigureAwait(false);
            while (true)
            {
                progress.Pages++;
                var held = Stopwatch.StartNew();
                var cursor = page.HasMore ? page.NextCursor : null;
                for (var i = 0; i < page.Data.Count; i++)
                {
                    // Read-ahead for slow consumers: the caller asked for another item and has held this
                    // page long enough that waiting for its end could let the cursor's idle window lapse.
                    if (cursor is not null && next is null && held.Elapsed >= ReadAheadAfter) next = _fetch(cursor, cts.Token);
                    progress.ItemsYielded++;
                    progress.LastId = page.ItemIds[i];
                    progress.LastUpdatedAt = page.ItemUpdatedAts[i];
                    yield return page.Data[i];
                }
                if (cursor is null) yield break;
                var pending = next ?? _fetch(cursor, cts.Token);
                next = null;
                page = await AwaitPageAsync(pending, progress).ConfigureAwait(false);
            }
        }
        finally
        {
            Release(next, cts);
        }
    }

    private async IAsyncEnumerable<Page<T>> IteratePagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var progress = new Progress();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        var page = await AwaitPageAsync(_fetch(null, cts.Token), progress).ConfigureAwait(false);
        while (true)
        {
            progress.Record(page);
            yield return page;
            if (!page.HasMore || page.NextCursor is not { } cursor) yield break;
            page = await AwaitPageAsync(_fetch(cursor, cts.Token), progress).ConfigureAwait(false);
        }
    }

    /// <summary>Awaits a page; adds the iteration's progress to a <see cref="CursorExpiredException"/>.</summary>
    private static async Task<Page<T>> AwaitPageAsync(Task<Page<T>> page, Progress progress)
    {
        try
        {
            return await page.ConfigureAwait(false);
        }
        catch (CursorExpiredException ex)
        {
            throw new CursorExpiredException(ex, progress.ItemsYielded, progress.Pages, progress.LastId, progress.LastUpdatedAt);
        }
    }

    /// <summary>Cancels and observes a read-ahead request the caller no longer needs, then disposes the token source.</summary>
    private static void Release(Task<Page<T>>? next, CancellationTokenSource cts)
    {
        if (next is not null && !next.IsCompleted)
        {
            cts.Cancel();
            _ = next.ContinueWith(t => { _ = t.Exception; cts.Dispose(); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        else
        {
            if (next is { IsFaulted: true }) _ = next.Exception;
            cts.Dispose();
        }
    }

    private sealed class Progress
    {
        public int ItemsYielded;
        public int Pages;
        public string? LastId;
        public string? LastUpdatedAt;

        public void Record(Page<T> page)
        {
            Pages++;
            ItemsYielded += page.Data.Count;
            if (page.Data.Count > 0)
            {
                LastId = page.ItemIds[page.Data.Count - 1];
                LastUpdatedAt = page.ItemUpdatedAts[page.Data.Count - 1];
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

    internal RequestHandle(ApiCore core, Request request, Func<string, T> parse, RequestOptions? options, string? idempotencyKey = null)
    {
        _core = core;
        Request = request;
        _parse = parse;
        _options = options;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>The <c>Idempotency-Key</c> sent with the write that created this request, else <c>null</c>.</summary>
    public string? IdempotencyKey { get; }

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
    /// throws <see cref="RequestPendingException"/> when the time is up or a poll fails.
    /// </summary>
    /// <param name="timeout">How long to wait. Default: the client timeout.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The operation's result.</returns>
    public Task<T> WaitAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        _core.WaitForResultAsync(Id, Request, _parse, timeout, _options, IdempotencyKey, cancellationToken);

    /// <summary>Checks once: returns the result if the request succeeded, throws its typed error if it failed, or throws <see cref="RequestPendingException"/> if it has not finished.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The operation's result.</returns>
    public async Task<T> ResultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await _core.GetRequestAsync(Id, null, _options, cancellationToken).ConfigureAwait(false);
            if (ApiCore.TryResolve(current.Request, _parse, out var result)) return result;
            throw new RequestPendingException(Id, current.Request, null, null, IdempotencyKey);
        }
        catch (DaapiException ex) when (ApiCore.AttachKey(ex, IdempotencyKey))
        {
            throw;
        }
    }
}
