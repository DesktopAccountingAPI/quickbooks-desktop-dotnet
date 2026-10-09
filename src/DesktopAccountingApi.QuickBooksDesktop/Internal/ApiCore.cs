using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DesktopAccountingApi.QuickBooksDesktop.Models;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>Static facts about one API operation, generated from the contract.</summary>
internal sealed class OperationInfo
{
    public OperationInfo(string id, string method, bool write, bool requiresEndUser, bool serverTimeout, bool queueTtl)
    {
        Id = id;
        Method = new HttpMethod(method);
        Write = write;
        RequiresEndUser = requiresEndUser;
        ServerTimeout = serverTimeout;
        QueueTtl = queueTtl;
    }

    public string Id { get; }
    public HttpMethod Method { get; }
    public bool Write { get; }
    public bool RequiresEndUser { get; }
    public bool ServerTimeout { get; }
    public bool QueueTtl { get; }
}

/// <summary>The HTTP engine shared by a client and its <c>ForEndUser</c> copies: headers, retries, error mapping, long polling.</summary>
internal sealed class ApiCore
{
    internal const string UserAgent = "desktopaccountingapi-dotnet/" + DesktopAccountingApiClient.SdkVersion;
    internal static readonly OperationInfo RequestsRetrieve = new("requests.retrieve", "GET", write: false, requiresEndUser: false, serverTimeout: false, queueTtl: false);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(8);

    private readonly HttpClient _http;
    private readonly string _authorization;

    internal ApiCore(HttpClient http, string apiKey, string baseUrl, string? endUserId, TimeSpan timeout, int maxRetries, TimeSpan? serverTimeout, Action<DaapiLogLevel, string>? logger)
    {
        _http = http;
        _authorization = "Bearer " + apiKey;
        BaseUrl = baseUrl;
        EndUserId = endUserId;
        Timeout = timeout;
        MaxRetries = maxRetries;
        ServerTimeout = serverTimeout;
        Logger = logger;
    }

    internal string BaseUrl { get; }
    internal string? EndUserId { get; }
    internal TimeSpan Timeout { get; }
    internal TimeSpan? TotalTimeout { get; set; }
    internal IReadOnlyList<KeyValuePair<string, string>> DefaultHeaders { get; set; } = Array.Empty<KeyValuePair<string, string>>();

    private static readonly HashSet<string> s_managedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Accept", "Content-Type", "User-Agent", "Daapi-End-User-Id", "Conductor-End-User-Id",
        "Idempotency-Key", "Daapi-Timeout-Seconds", "Prefer", "Daapi-Queue-Ttl-Seconds",
    };

    /// <summary>The caller's default headers without the ones the SDK manages.</summary>
    internal static IReadOnlyList<KeyValuePair<string, string>> CopyDefaultHeaders(IDictionary<string, string>? headers)
    {
        if (headers is null) return Array.Empty<KeyValuePair<string, string>>();
        var list = new List<KeyValuePair<string, string>>();
        foreach (var h in headers)
        {
            if (string.IsNullOrEmpty(h.Key) || h.Value is null) throw new DaapiException("DefaultHeaders must map header names to values.");
            if (!s_managedHeaders.Contains(h.Key)) list.Add(h);
        }
        return list;
    }
    internal int MaxRetries { get; }
    internal TimeSpan? ServerTimeout { get; }
    internal Action<DaapiLogLevel, string>? Logger { get; }

    /// <summary>Waits between retries. Tests replace it to skip real delays.</summary>
    internal Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = (d, ct) => Task.Delay(d, ct);

    /// <summary>Returns a value in [0, 1) for backoff jitter.</summary>
    internal Func<double> Jitter { get; set; } = NextJitter;

    private static readonly object s_randomLock = new();
    private static readonly Random s_random = new();

    private static double NextJitter()
    {
        lock (s_randomLock) return s_random.NextDouble();
    }

    internal ApiCore WithEndUser(string endUserId) =>
        new(_http, _authorization.Substring("Bearer ".Length), BaseUrl, endUserId, Timeout, MaxRetries, ServerTimeout, Logger)
        {
            Delay = Delay,
            Jitter = Jitter,
            TotalTimeout = TotalTimeout,
            DefaultHeaders = DefaultHeaders,
        };

    internal static string PathSegment(string value, string name)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException($"{name} must be a non-empty string.", name);
        return Uri.EscapeDataString(value);
    }

    // ---------------------------------------------------------------- calls

    private sealed class Call
    {
        public Call(OperationInfo op, string? endUserId, string? idempotencyKey, TimeSpan timeout, TimeSpan? totalTimeout, int maxRetries, TimeSpan? serverTimeout, TimeSpan? queueTtl, bool async)
        {
            Op = op;
            TotalTimeout = totalTimeout;
            EndUserId = endUserId;
            IdempotencyKey = idempotencyKey;
            Timeout = timeout;
            MaxRetries = maxRetries;
            ServerTimeout = serverTimeout;
            QueueTtl = queueTtl;
            Async = async;
            Clock = Stopwatch.StartNew();
        }

        public OperationInfo Op { get; }
        public string? EndUserId { get; }
        public string? IdempotencyKey { get; }
        public TimeSpan Timeout { get; }
        public TimeSpan? TotalTimeout { get; }
        public int MaxRetries { get; }
        public TimeSpan? ServerTimeout { get; }
        public TimeSpan? QueueTtl { get; }
        public bool Async { get; }
        public Stopwatch Clock { get; }

        /// <summary>Time left of the total timeout, or <c>null</c> without one.</summary>
        public TimeSpan? Remaining => TotalTimeout is { } total ? total - Clock.Elapsed : null;

        /// <summary>The wait budget for a pending request: the total timeout if set, else the attempt timeout.</summary>
        public TimeSpan PendingBudget => TotalTimeout ?? Timeout;
    }

    private Call Begin(OperationInfo op, RequestOptions? options, bool async, bool totalTimeout = true)
    {
        var endUserId = options?.EndUserId ?? EndUserId;
        if (op.RequiresEndUser && string.IsNullOrEmpty(endUserId))
        {
            throw new DaapiException($"{op.Id} needs an end user: set ClientOptions.EndUserId, use client.ForEndUser(\"eu_...\"), or pass RequestOptions.EndUserId.");
        }
        var timeout = options?.Timeout ?? Timeout;
        if (timeout <= TimeSpan.Zero) throw new DaapiException("Timeout must be positive.");
        var total = totalTimeout ? options?.TotalTimeout ?? TotalTimeout : null;
        if (total <= TimeSpan.Zero) throw new DaapiException("TotalTimeout must be positive.");
        var maxRetries = options?.MaxRetries ?? MaxRetries;
        if (maxRetries < 0) throw new DaapiException("MaxRetries must be zero or more.");
        var key = op.Write ? (options?.IdempotencyKey ?? Guid.NewGuid().ToString("D")) : null;
        return new Call(op, op.RequiresEndUser ? endUserId : null, key, timeout, total, maxRetries, options?.ServerTimeout ?? ServerTimeout, options?.QueueTtl, async);
    }

    private sealed class RawResponse
    {
        public RawResponse(int status, IReadOnlyDictionary<string, string> headers, string body, string? requestId = null)
        {
            Status = status;
            Headers = headers;
            Body = body;
            RequestId = requestId ?? Header("Daapi-Request-Id");
        }

        public int Status { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public string Body { get; }

        /// <summary>The request that produced the body: <c>Daapi-Request-Id</c>, or the long-polled request's ID.</summary>
        public string? RequestId { get; }
        public bool Success => Status >= 200 && Status < 300;
        public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;
    }

    internal async Task<ApiResponse<T>> SendAsync<T>(OperationInfo op, string path, InputObject? query, object? body, RequestOptions? options, CancellationToken cancellationToken)
    {
        var call = Begin(op, options, async: false);
        try
        {
            var (json, raw) = await SendForResultAsync(call, path, query?.ToQuery(), Serialize(body), "application/json", "application/json", cancellationToken).ConfigureAwait(false);
            return new ApiResponse<T>(Parse<T>(json), raw.Status, raw.Headers, call.IdempotencyKey, raw.RequestId);
        }
        catch (DaapiException ex) when (AttachKey(ex, call.IdempotencyKey))
        {
            throw;
        }
    }

    internal async Task<string> SendXmlAsync(OperationInfo op, string path, string xml, RequestOptions? options, CancellationToken cancellationToken)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var call = Begin(op, options, async: false);
        try
        {
            var (text, _) = await SendForResultAsync(call, path, null, Encoding.UTF8.GetBytes(xml), "application/xml", "application/xml", cancellationToken).ConfigureAwait(false);
            return text;
        }
        catch (DaapiException ex) when (AttachKey(ex, call.IdempotencyKey))
        {
            throw;
        }
    }

    internal Pager<T> Paginate<T>(OperationInfo op, string path, InputObject? query, RequestOptions? options, CancellationToken cancellationToken)
    {
        // Continue requests send only the cursor and, if the caller set one, the limit.
        var limit = query?.QueryValue("limit");
        return new Pager<T>(async (cursor, ct) =>
        {
            var call = Begin(op, options, async: false);
            List<KeyValuePair<string, string>> pairs;
            if (cursor is null)
            {
                pairs = query?.ToQuery() ?? new List<KeyValuePair<string, string>>();
            }
            else
            {
                pairs = new List<KeyValuePair<string, string>> { new("cursor", cursor) };
                if (limit is not null) pairs.Add(new KeyValuePair<string, string>("limit", limit));
            }
            var (json, raw) = await SendForResultAsync(call, path, pairs, null, null, "application/json", ct).ConfigureAwait(false);
            return Page<T>.Parse(json, raw.RequestId);
        }, cancellationToken);
    }

    internal Task<RequestHandle<T>> EnqueueAsync<T>(OperationInfo op, string path, InputObject? query, object? body, RequestOptions? options, CancellationToken cancellationToken) =>
        EnqueueCoreAsync(op, path, query, body, Parse<T>, options, cancellationToken);

    internal Task<RequestHandle<Page<T>>> EnqueuePageAsync<T>(OperationInfo op, string path, InputObject? query, RequestOptions? options, CancellationToken cancellationToken) =>
        EnqueueCoreAsync(op, path, query, null, json => Page<T>.Parse(json, null), options, cancellationToken);

    private async Task<RequestHandle<T>> EnqueueCoreAsync<T>(OperationInfo op, string path, InputObject? query, object? body, Func<string, T> parse, RequestOptions? options, CancellationToken cancellationToken)
    {
        var call = Begin(op, options, async: true);
        try
        {
            var raw = await ExecuteAsync(call, op.Method, BuildUrl(path, query?.ToQuery()), Serialize(body), body is null ? null : "application/json", "application/json", call.Timeout, cancellationToken).ConfigureAwait(false);
            if (!raw.Success) throw ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
            if (raw.Status != 202) throw new DaapiException($"{op.Id}: expected 202 Accepted for an async-mode request, got {raw.Status}.");
            var request = Parse<Request>(raw.Body);
            return new RequestHandle<T>(this, request, parse, options, call.IdempotencyKey);
        }
        catch (DaapiException ex) when (AttachKey(ex, call.IdempotencyKey))
        {
            throw;
        }
    }

    /// <summary>Exception filter: records the write's idempotency key on the exception and never catches it.</summary>
    internal static bool AttachKey(DaapiException ex, string? key)
    {
        if (key is not null && ex.IdempotencyKey is null) ex.IdempotencyKey = key;
        return false;
    }

    // ------------------------------------------------------- request polling

    internal async Task<(Request Request, IReadOnlyDictionary<string, string> Headers, int Status)> GetRequestAsync(string requestId, int? waitSeconds, RequestOptions? options, CancellationToken cancellationToken)
    {
        // A long poll carries its caller's remaining budget as TotalTimeout (codex review #15): every
        // attempt, retry and backoff stays inside it. Without one it is not cut short.
        var call = Begin(RequestsRetrieve, options, async: false, totalTimeout: waitSeconds is null || options?.TotalTimeout is not null);
        var query = new List<KeyValuePair<string, string>>();
        if (waitSeconds is { } w) query.Add(new KeyValuePair<string, string>("waitSeconds", w.ToString(CultureInfo.InvariantCulture)));
        var attemptTimeout = waitSeconds is { } s ? TimeSpan.FromSeconds(s + 10) : call.Timeout;
        var raw = await ExecuteAsync(call, HttpMethod.Get, BuildUrl("/v1/requests/" + PathSegment(requestId, "requestId"), query), null, null, "application/json", attemptTimeout, cancellationToken).ConfigureAwait(false);
        if (!raw.Success) throw ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        return (Parse<Request>(raw.Body), raw.Headers, raw.Status);
    }

    internal async Task<T> WaitForResultAsync<T>(string requestId, Request? last, Func<string, T> parse, TimeSpan? timeout, RequestOptions? options, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var budget = timeout ?? options?.TotalTimeout ?? TotalTimeout ?? options?.Timeout ?? Timeout;
        try
        {
            var (result, _, _) = await PollAsync(requestId, last, parse, Stopwatch.StartNew(), budget, options, null, idempotencyKey, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (DaapiException ex) when (AttachKey(ex, idempotencyKey))
        {
            // The request's own typed exceptions (failed, canceled, outcome_unknown) carry the key too.
            throw;
        }
    }

    /// <summary>
    /// Long-polls until the request settles or the budget ends. A settled request returns its result
    /// or throws its own typed exception. Anything else that ends the wait (the budget, or a poll that
    /// failed: 429, 5xx, 404, network, timeout) throws <see cref="RequestPendingException"/>; the
    /// failed poll's own retryable exception would invite a duplicate write (Fable review F-1).
    /// </summary>
    private async Task<(T Result, IReadOnlyDictionary<string, string> Headers, int Status)> PollAsync<T>(string requestId, Request? last, Func<string, T> parse, Stopwatch clock, TimeSpan budget, RequestOptions? options, ApiException? timeoutError, string? idempotencyKey, CancellationToken cancellationToken)
    {
        while (true)
        {
            var remaining = budget - clock.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new RequestPendingException(requestId, last, timeoutError, null, idempotencyKey);
            var wait = (int)Math.Min(60, Math.Ceiling(remaining.TotalSeconds));
            Log(DaapiLogLevel.Info, $"Waiting up to {wait} s for request {requestId}.");
            var pollOptions = new RequestOptions { MaxRetries = options?.MaxRetries, Timeout = options?.Timeout, TotalTimeout = remaining };
            Request request;
            IReadOnlyDictionary<string, string> headers;
            int status;
            try
            {
                (request, headers, status) = await GetRequestAsync(requestId, wait, pollOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (DaapiException ex)
            {
                throw new RequestPendingException(requestId, last, timeoutError, ex, idempotencyKey);
            }
            // An answer that arrives after the budget is not returned, settled or not (codex re-review #15).
            if (clock.Elapsed > budget) throw new RequestPendingException(requestId, request, timeoutError, null, idempotencyKey);
            if (TryResolve(request, parse, out var result)) return (result, headers, status);
            last = request;
        }
    }

    /// <summary>Returns true with the result for a succeeded request; throws for failed, canceled and outcome_unknown; false otherwise.</summary>
    internal static bool TryResolve<T>(Request request, Func<string, T> parse, out T result)
    {
        result = default!;
        switch (request.Status)
        {
            case RequestStatus.Succeeded:
                // QuickBooks answered, but the API could not map the answer (for example
                // QBD_RESPONSE_UNREADABLE, outcome applied): throw that catalog error (codex review #4).
                if (request.Error is not null) throw ErrorFactory.Create(request.Error.HttpStatusCode, request.Error, null, $"Request {request.Id} succeeded, but its result could not be read.");
                if (request.Result is not { } value || value.ValueKind == JsonValueKind.Null)
                {
                    throw new DaapiException(request.ResultExpired
                        ? $"Request {request.Id} succeeded, but its result is past the retention period."
                        : $"Request {request.Id} succeeded without a result.");
                }
                result = parse(value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText());
                return true;
            case RequestStatus.Failed:
            case RequestStatus.Canceled:
            case RequestStatus.OutcomeUnknown:
                throw ErrorFactory.Create(request.Error?.HttpStatusCode, request.Error, null, $"Request {request.Id} ended with status {request.Status}.");
            default:
                return false;
        }
    }

    // ------------------------------------------------------------ transport

    private async Task<(string Json, RawResponse Raw)> SendForResultAsync(Call call, string path, List<KeyValuePair<string, string>>? query, byte[]? body, string? contentType, string accept, CancellationToken cancellationToken)
    {
        var raw = await ExecuteAsync(call, call.Op.Method, BuildUrl(path, query), body, body is null ? null : contentType, accept, call.Timeout, cancellationToken).ConfigureAwait(false);
        if (raw.Success) return (raw.Body, raw);
        var error = ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        // 504 QBD_REQUEST_TIMEOUT: the request reached QuickBooks' queue and may still run. Never
        // resubmit; long-poll the request resource until this call's deadline. This holds for reads
        // (outcome not_applicable) as well as writes (outcome pending).
        if (PendingRequestId(raw.Status, error) is { } requestId)
        {
            Log(DaapiLogLevel.Info, $"{call.Op.Id} timed out on the server; collecting request {requestId} without resending.");
            var options = new RequestOptions { MaxRetries = call.MaxRetries, Timeout = call.Timeout };
            var (json, headers, status) = await PollAsync(requestId, null, s => s, call.Clock, call.PendingBudget, options, error, call.IdempotencyKey, cancellationToken).ConfigureAwait(false);
            // The result belongs to the request that timed out, not to the poll that collected it.
            return (json, new RawResponse(status, headers, json, requestId));
        }
        Log(DaapiLogLevel.Warning, $"{call.Op.Id} failed: HTTP {raw.Status} {error.Code} (request {error.RequestId}).");
        throw error;
    }

    private async Task<RawResponse> ExecuteAsync(Call call, HttpMethod method, string url, byte[]? body, string? contentType, string accept, TimeSpan attemptTimeout, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var thisAttempt = attemptTimeout;
            if (call.Remaining is { } remaining)
            {
                if (remaining <= TimeSpan.Zero) throw new ApiTimeoutException($"{call.Op.Id}: the call's total timeout ended before a response arrived.", null);
                if (remaining < thisAttempt) thisAttempt = remaining;
            }
            var started = Stopwatch.StartNew();
            DaapiException? failure;
            using (var request = BuildRequest(call, method, url, body, contentType, accept))
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(thisAttempt);
                try
                {
                    using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);
#if NET8_0_OR_GREATER
                    var text = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
#else
                    var text = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                    var raw = new RawResponse((int)response.StatusCode, CollectHeaders(response), text);
                    Log(DaapiLogLevel.Debug, $"{method} {PathOf(url)} -> {raw.Status} in {started.ElapsedMilliseconds} ms (request {raw.Header("Daapi-Request-Id")})");
                    if (raw.Success || attempt >= call.MaxRetries || !ShouldRetry(raw)) return raw;
                    var delay = RetryAfter(raw.Header("Retry-After")) ?? Backoff(attempt);
                    if (call.Remaining is { } left && delay >= left) return raw;
                    Log(DaapiLogLevel.Info, $"Retrying {call.Op.Id} after HTTP {raw.Status} in {delay.TotalMilliseconds:F0} ms (attempt {attempt + 2} of {call.MaxRetries + 1}).");
                    await Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    failure = new ApiTimeoutException($"{call.Op.Id}: no response within {thisAttempt.TotalSeconds:0.###} s.", ex);
                }
                catch (HttpRequestException ex)
                {
                    failure = new ApiConnectionException($"{call.Op.Id}: connection error: {ex.Message}", ex);
                }
                catch (IOException ex)
                {
                    failure = new ApiConnectionException($"{call.Op.Id}: connection error: {ex.Message}", ex);
                }
            }
            if (attempt >= call.MaxRetries) throw failure;
            var wait = Backoff(attempt);
            if (call.Remaining is { } budget && wait >= budget) throw failure;
            Log(DaapiLogLevel.Info, $"Retrying {call.Op.Id} after a network error in {wait.TotalMilliseconds:F0} ms (attempt {attempt + 2} of {call.MaxRetries + 1}).");
            await Delay(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    private HttpRequestMessage BuildRequest(Call call, HttpMethod method, string url, byte[]? body, string? contentType, string accept)
    {
        var request = new HttpRequestMessage(method, url);
        var h = request.Headers;
        foreach (var header in DefaultHeaders) h.TryAddWithoutValidation(header.Key, header.Value);
        h.TryAddWithoutValidation("Authorization", _authorization);
        h.TryAddWithoutValidation("User-Agent", UserAgent);
        h.TryAddWithoutValidation("Accept", accept);
        if (call.EndUserId is not null) h.TryAddWithoutValidation("Daapi-End-User-Id", call.EndUserId);
        if (call.IdempotencyKey is not null) h.TryAddWithoutValidation("Idempotency-Key", call.IdempotencyKey);
        if (call.Op.ServerTimeout && call.ServerTimeout is { } st) h.TryAddWithoutValidation("Daapi-Timeout-Seconds", Seconds(st));
        if (call.Async)
        {
            h.TryAddWithoutValidation("Prefer", "respond-async");
            if (call.Op.QueueTtl && call.QueueTtl is { } ttl) h.TryAddWithoutValidation("Daapi-Queue-Ttl-Seconds", Seconds(ttl));
        }
        if (body is not null)
        {
            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/json") { CharSet = "utf-8" };
            request.Content = content;
        }
        return request;
    }

    private static string Seconds(TimeSpan value) => ((long)Math.Ceiling(value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The request to long-poll after <c>504 QBD_REQUEST_TIMEOUT</c>, else null. Same rule in every SDK:
    /// HTTP 504, code <c>QBD_REQUEST_TIMEOUT</c> and a non-empty <c>details.requestId</c>, whatever the outcome.
    /// </summary>
    private static string? PendingRequestId(int status, ApiException error) =>
        status == 504 && error.Code == ErrorCodes.QbdRequestTimeout &&
        error.Details.TryGetValue("requestId", out var rid) && rid.ValueKind == JsonValueKind.String &&
        rid.GetString() is { Length: > 0 } id
            ? id
            : null;

    /// <summary>Retry 429 and 5xx with <c>Daapi-Should-Retry: true</c>; never when the header says false, the outcome is unknown or pending, or the request is still running after the server timeout.</summary>
    private static bool ShouldRetry(RawResponse raw)
    {
        var header = raw.Header("Daapi-Should-Retry");
        if (string.Equals(header, "false", StringComparison.OrdinalIgnoreCase)) return false;
        var error = ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        if (error.Outcome is ErrorOutcome.Unknown or ErrorOutcome.Pending) return false;
        if (PendingRequestId(raw.Status, error) is not null) return false;
        if (raw.Status == 429) return true;
        return raw.Status >= 500 && string.Equals(header, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>0.5 s * 2^attempt, capped at 8 s, with jitter (50-100 % of the step).</summary>
    internal TimeSpan Backoff(int attempt)
    {
        var step = Math.Min(MaxBackoff.TotalSeconds, 0.5 * Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(step * (0.5 + (0.5 * Jitter())));
    }

    /// <summary>Parses <c>Retry-After</c> as seconds (including 0) or an HTTP date.</summary>
    internal static TimeSpan? RetryAfter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds >= 0 && !double.IsInfinity(seconds) ? TimeSpan.FromSeconds(seconds) : null;
        }
        if (DateTimeOffset.TryParseExact(value!.Trim(), "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            var delta = date - DateTimeOffset.UtcNow;
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }
        return null;
    }

    private static Dictionary<string, string> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in response.Headers) headers[h.Key] = string.Join(", ", h.Value);
        if (response.Content is not null)
        {
            foreach (var h in response.Content.Headers) headers[h.Key] = string.Join(", ", h.Value);
        }
        return headers;
    }

    private string BuildUrl(string path, List<KeyValuePair<string, string>>? query)
    {
        var sb = new StringBuilder(BaseUrl).Append(path);
        if (query is { Count: > 0 })
        {
            sb.Append('?');
            sb.Append(string.Join("&", query.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value))));
        }
        return sb.ToString();
    }

    private string PathOf(string url)
    {
        var path = url.Substring(BaseUrl.Length);
        var q = path.IndexOf('?');
        return q >= 0 ? path.Substring(0, q) : path;
    }

    private void Log(DaapiLogLevel level, string message) => Logger?.Invoke(level, message);

    private static byte[]? Serialize(object? body) =>
        body is null ? null : JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), DesktopAccountingApiJson.Options);

    internal static T Parse<T>(string json)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(json, DesktopAccountingApiJson.Options);
            return value is null ? throw new DaapiException($"Expected a {typeof(T).Name} in the response, got null.") : value;
        }
        catch (JsonException ex)
        {
            throw new DaapiException($"Could not parse the response as {typeof(T).Name}: {ex.Message}", ex);
        }
    }
}
