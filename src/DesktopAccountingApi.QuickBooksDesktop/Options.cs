using System;
using System.Net.Http;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>Log levels passed to <see cref="ClientOptions.Logger"/>.</summary>
public enum DaapiLogLevel
{
    /// <summary>Every HTTP attempt: method, path, status, duration and request ID.</summary>
    Debug,

    /// <summary>Retries and long polling.</summary>
    Info,

    /// <summary>Calls that end in an error.</summary>
    Warning,
}

/// <summary>Settings for <see cref="DesktopAccountingApiClient"/>.</summary>
public sealed class ClientOptions
{
    /// <summary>Secret key (<c>sk_live_...</c> or <c>sk_test_...</c>). Defaults to the <c>DAAPI_SECRET_KEY</c> environment variable. Validated locally (format and checksum) when the client is created.</summary>
    public string? ApiKey { get; set; }

    /// <summary>API base URL. Defaults to the <c>DAAPI_BASE_URL</c> environment variable, else <c>https://api.desktopaccountingapi.com</c>. May include a path prefix.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Default end user (<c>eu_...</c>) for QuickBooks Desktop operations. Override per call with <see cref="RequestOptions.EndUserId"/> or use <see cref="DesktopAccountingApiClient.ForEndUser"/>.</summary>
    public string? EndUserId { get; set; }

    /// <summary>Client-side timeout of each HTTP attempt, and the total time the SDK waits for a pending request after <c>504 QBD_REQUEST_TIMEOUT</c>. Default 100 seconds (server default 90 seconds plus 10).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Retries after network errors, <c>429</c> and retryable <c>5xx</c> responses. Default 2; 0 disables retries.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Server-side wait budget sent as <c>Daapi-Timeout-Seconds</c> on operations that accept it (whole seconds, rounded up). Unset uses the server default.</summary>
    public TimeSpan? ServerTimeout { get; set; }

    /// <summary>An <see cref="System.Net.Http.HttpClient"/> to send requests with. The SDK does not dispose it and does not change its settings; keep its <c>Timeout</c> above the SDK's.</summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>An <see cref="System.Net.Http.HttpMessageHandler"/> for the SDK's own <see cref="System.Net.Http.HttpClient"/> (proxies, test doubles, instrumentation). Ignored when <see cref="HttpClient"/> is set. Not disposed by the SDK.</summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>Receives log lines. The SDK never logs the API key, the <c>Authorization</c> header or request and response bodies.</summary>
    public Action<DaapiLogLevel, string>? Logger { get; set; }
}

/// <summary>Per-call overrides. Every property is optional.</summary>
public sealed class RequestOptions
{
    /// <summary>End user (<c>eu_...</c>) for this call. Sent as <c>Daapi-End-User-Id</c> on QuickBooks Desktop operations only.</summary>
    public string? EndUserId { get; set; }

    /// <summary>Idempotency key for a write. Default: a new UUID per call, reused on every retry of that call.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Overrides <see cref="ClientOptions.Timeout"/>.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Overrides <see cref="ClientOptions.MaxRetries"/>.</summary>
    public int? MaxRetries { get; set; }

    /// <summary>Overrides <see cref="ClientOptions.ServerTimeout"/> (<c>Daapi-Timeout-Seconds</c>).</summary>
    public TimeSpan? ServerTimeout { get; set; }

    /// <summary>Async mode only: how long the request may wait in the queue (<c>Daapi-Queue-Ttl-Seconds</c>, whole seconds).</summary>
    public TimeSpan? QueueTtl { get; set; }
}
