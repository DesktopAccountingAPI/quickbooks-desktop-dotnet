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
        new(_http, _authorization.Substring("Bearer ".Length), BaseUrl, endUserId, Timeout, MaxRetries, ServerTimeout, Logger) { Delay = Delay, Jitter = Jitter };

    internal static string PathSegment(string value, string name)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException($"{name} must be a non-empty string.", name);
        return Uri.EscapeDataString(value);
    }

    // ---------------------------------------------------------------- calls

    private sealed class Call
    {
        public Call(OperationInfo op, string? endUserId, string? idempotencyKey, TimeSpan timeout, int maxRetries, TimeSpan? serverTimeout, TimeSpan? queueTtl, bool async)
        {
            Op = op;
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
        public int MaxRetries { get; }
        public TimeSpan? ServerTimeout { get; }
        public TimeSpan? QueueTtl { get; }
        public bool Async { get; }
        public Stopwatch Clock { get; }
    }

    private Call Begin(OperationInfo op, RequestOptions? options, bool async)
    {
        var endUserId = options?.EndUserId ?? EndUserId;
        if (op.RequiresEndUser && string.IsNullOrEmpty(endUserId))
        {
            throw new DaapiException($"{op.Id} needs an end user: set ClientOptions.EndUserId, use client.ForEndUser(\"eu_...\"), or pass RequestOptions.EndUserId.");
        }
        var timeout = options?.Timeout ?? Timeout;
        if (timeout <= TimeSpan.Zero) throw new DaapiException("Timeout must be positive.");
        var maxRetries = options?.MaxRetries ?? MaxRetries;
        if (maxRetries < 0) throw new DaapiException("MaxRetries must be zero or more.");
        var key = op.Write ? (options?.IdempotencyKey ?? Guid.NewGuid().ToString("D")) : null;
        return new Call(op, op.RequiresEndUser ? endUserId : null, key, timeout, maxRetries, options?.ServerTimeout ?? ServerTimeout, options?.QueueTtl, async);
    }

    private sealed class RawResponse
    {
        public RawResponse(int status, IReadOnlyDictionary<string, string> headers, string body)
        {
            Status = status;
            Headers = headers;
            Body = body;
        }

        public int Status { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public string Body { get; }
        public bool Success => Status >= 200 && Status < 300;
        public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;
    }

    internal async Task<ApiResponse<T>> SendAsync<T>(OperationInfo op, string path, InputObject? query, object? body, RequestOptions? options, CancellationToken cancellationToken)
    {
        var call = Begin(op, options, async: false);
        var (json, raw) = await SendForResultAsync(call, path, query?.ToQuery(), Serialize(body), "application/json", "application/json", cancellationToken).ConfigureAwait(false);
        return new ApiResponse<T>(Parse<T>(json), raw.Status, raw.Headers);
    }

    internal async Task<string> SendXmlAsync(OperationInfo op, string path, string xml, RequestOptions? options, CancellationToken cancellationToken)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var call = Begin(op, options, async: false);
        var (text, _) = await SendForResultAsync(call, path, null, Encoding.UTF8.GetBytes(xml), "application/xml", "application/xml", cancellationToken).ConfigureAwait(false);
        return text;
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
            return Page<T>.Parse(json, raw.Header("Daapi-Request-Id"));
        }, cancellationToken);
    }

    internal Task<RequestHandle<T>> EnqueueAsync<T>(OperationInfo op, string path, InputObject? query, object? body, RequestOptions? options, CancellationToken cancellationToken) =>
        EnqueueCoreAsync(op, path, query, body, Parse<T>, options, cancellationToken);

    internal Task<RequestHandle<Page<T>>> EnqueuePageAsync<T>(OperationInfo op, string path, InputObject? query, RequestOptions? options, CancellationToken cancellationToken) =>
        EnqueueCoreAsync(op, path, query, null, json => Page<T>.Parse(json, null), options, cancellationToken);

    private async Task<RequestHandle<T>> EnqueueCoreAsync<T>(OperationInfo op, string path, InputObject? query, object? body, Func<string, T> parse, RequestOptions? options, CancellationToken cancellationToken)
    {
        var call = Begin(op, options, async: true);
        var raw = await ExecuteAsync(call, op.Method, BuildUrl(path, query?.ToQuery()), Serialize(body), body is null ? null : "application/json", "application/json", call.Timeout, cancellationToken).ConfigureAwait(false);
        if (!raw.Success) throw ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        if (raw.Status != 202) throw new DaapiException($"{op.Id}: expected 202 Accepted for an async-mode request, got {raw.Status}.");
        var request = Parse<Request>(raw.Body);
        return new RequestHandle<T>(this, request, parse, options);
    }

    // ------------------------------------------------------- request polling

    internal async Task<(Request Request, IReadOnlyDictionary<string, string> Headers, int Status)> GetRequestAsync(string requestId, int? waitSeconds, RequestOptions? options, CancellationToken cancellationToken)
    {
        var call = Begin(RequestsRetrieve, options, async: false);
        var query = new List<KeyValuePair<string, string>>();
        if (waitSeconds is { } w) query.Add(new KeyValuePair<string, string>("waitSeconds", w.ToString(CultureInfo.InvariantCulture)));
        var attemptTimeout = waitSeconds is { } s ? TimeSpan.FromSeconds(s + 10) : call.Timeout;
        var raw = await ExecuteAsync(call, HttpMethod.Get, BuildUrl("/v1/requests/" + PathSegment(requestId, "requestId"), query), null, null, "application/json", attemptTimeout, cancellationToken).ConfigureAwait(false);
        if (!raw.Success) throw ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        return (Parse<Request>(raw.Body), raw.Headers, raw.Status);
    }

    internal async Task<T> WaitForResultAsync<T>(string requestId, Request? last, Func<string, T> parse, TimeSpan? timeout, RequestOptions? options, CancellationToken cancellationToken)
    {
        var budget = timeout ?? options?.Timeout ?? Timeout;
        var (result, _, _) = await PollAsync(requestId, last, parse, Stopwatch.StartNew(), budget, options, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<(T Result, IReadOnlyDictionary<string, string> Headers, int Status)> PollAsync<T>(string requestId, Request? last, Func<string, T> parse, Stopwatch clock, TimeSpan budget, RequestOptions? options, CancellationToken cancellationToken)
    {
        while (true)
        {
            var remaining = budget - clock.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new RequestPendingException(requestId, last);
            var wait = (int)Math.Min(60, Math.Ceiling(remaining.TotalSeconds));
            Log(DaapiLogLevel.Info, $"Waiting up to {wait} s for request {requestId}.");
            var pollOptions = new RequestOptions { MaxRetries = options?.MaxRetries, Timeout = options?.Timeout };
            var (request, headers, status) = await GetRequestAsync(requestId, wait, pollOptions, cancellationToken).ConfigureAwait(false);
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
        // resubmit; long-poll the request resource until this call's deadline.
        if (error.Code == ErrorCodes.QbdRequestTimeout && error.Outcome == ErrorOutcome.Pending &&
            error.Details.TryGetValue("requestId", out var rid) && rid.ValueKind == JsonValueKind.String)
        {
            var requestId = rid.GetString()!;
            Log(DaapiLogLevel.Info, $"{call.Op.Id} timed out on the server; collecting request {requestId} without resending.");
            var options = new RequestOptions { MaxRetries = call.MaxRetries, Timeout = call.Timeout };
            var (json, headers, status) = await PollAsync(requestId, null, s => s, call.Clock, call.Timeout, options, cancellationToken).ConfigureAwait(false);
            return (json, new RawResponse(status, headers, json));
        }
        Log(DaapiLogLevel.Warning, $"{call.Op.Id} failed: HTTP {raw.Status} {error.Code} (request {error.RequestId}).");
        throw error;
    }

    private async Task<RawResponse> ExecuteAsync(Call call, HttpMethod method, string url, byte[]? body, string? contentType, string accept, TimeSpan attemptTimeout, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var started = Stopwatch.StartNew();
            DaapiException? failure;
            using (var request = BuildRequest(call, method, url, body, contentType, accept))
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(attemptTimeout);
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
                    Log(DaapiLogLevel.Info, $"Retrying {call.Op.Id} after HTTP {raw.Status} in {delay.TotalMilliseconds:F0} ms (attempt {attempt + 2} of {call.MaxRetries + 1}).");
                    await Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    failure = new ApiTimeoutException($"{call.Op.Id}: no response within {attemptTimeout.TotalSeconds:0.###} s.", ex);
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
            Log(DaapiLogLevel.Info, $"Retrying {call.Op.Id} after a network error in {wait.TotalMilliseconds:F0} ms (attempt {attempt + 2} of {call.MaxRetries + 1}).");
            await Delay(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    private HttpRequestMessage BuildRequest(Call call, HttpMethod method, string url, byte[]? body, string? contentType, string accept)
    {
        var request = new HttpRequestMessage(method, url);
        var h = request.Headers;
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

    /// <summary>Retry 429 and 5xx with <c>Daapi-Should-Retry: true</c>; never when the header says false or the outcome is unknown or pending.</summary>
    private static bool ShouldRetry(RawResponse raw)
    {
        var header = raw.Header("Daapi-Should-Retry");
        if (string.Equals(header, "false", StringComparison.OrdinalIgnoreCase)) return false;
        var error = ErrorFactory.FromResponse(raw.Status, raw.Headers, raw.Body);
        if (error.Outcome is ErrorOutcome.Unknown or ErrorOutcome.Pending) return false;
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
