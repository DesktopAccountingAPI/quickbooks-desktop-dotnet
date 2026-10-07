using System;
using System.Net.Http;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>
/// Client for the Desktop Accounting API. Resources mirror the API's operation IDs:
/// <c>client.Qbd.Invoices.ListAsync(...)</c>, <c>client.EndUsers.CreateAsync(...)</c>,
/// <c>client.Requests.RetrieveAsync(...)</c>. The client is thread-safe; create one and reuse it.
/// </summary>
public sealed partial class DesktopAccountingApiClient : IDisposable
{
    /// <summary>This SDK's version.</summary>
    public const string SdkVersion = "0.2.0";

    /// <summary>The API contract version this SDK was generated from.</summary>
    public const string ApiVersion = "1.0.0";

    /// <summary>SHA-256 of the OpenAPI contract this SDK was generated from.</summary>
    public const string ContractSha256 = "6f5ac28d7c33ac90aa7e2c15d88efd50e8e41c808bcc5489f0c8b1cb7833326a";

    /// <summary>Production API base URL.</summary>
    public const string DefaultBaseUrl = "https://api.desktopaccountingapi.com";

    private readonly HttpClient? _ownedHttpClient;

    /// <summary>Creates a client. Reads <c>DAAPI_SECRET_KEY</c> and <c>DAAPI_BASE_URL</c> for settings you leave unset.</summary>
    /// <param name="options">Client settings.</param>
    /// <exception cref="DaapiException">The API key is missing or malformed, or a setting is invalid.</exception>
    public DesktopAccountingApiClient(ClientOptions? options = null)
    {
        options ??= new ClientOptions();
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("DAAPI_SECRET_KEY");
        if (apiKey is null)
        {
            throw new DaapiException("No API key: pass ClientOptions.ApiKey or set the DAAPI_SECRET_KEY environment variable to your secret key (sk_live_... or sk_test_...).");
        }
        ApiKeys.Validate(apiKey);
        var baseUrl = NormalizeBaseUrl(options.BaseUrl ?? Environment.GetEnvironmentVariable("DAAPI_BASE_URL") ?? DefaultBaseUrl);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new DaapiException($"BaseUrl must be an absolute http(s) URL, got \"{baseUrl}\".");
        }
        if (options.Timeout <= TimeSpan.Zero) throw new DaapiException("Timeout must be positive.");
        if (options.TotalTimeout <= TimeSpan.Zero) throw new DaapiException("TotalTimeout must be positive.");
        if (options.MaxRetries < 0) throw new DaapiException("MaxRetries must be zero or more.");

        HttpClient http;
        if (options.HttpClient is not null)
        {
            http = options.HttpClient;
        }
        else
        {
            _ownedHttpClient = options.HttpMessageHandler is not null
                ? new HttpClient(options.HttpMessageHandler, disposeHandler: false)
                : new HttpClient(CreateHandler(), disposeHandler: true);
            _ownedHttpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            http = _ownedHttpClient;
        }
        Core = new ApiCore(http, apiKey, baseUrl, options.EndUserId, options.Timeout, options.MaxRetries, options.ServerTimeout, options.Logger)
        {
            TotalTimeout = options.TotalTimeout,
            DefaultHeaders = ApiCore.CopyDefaultHeaders(options.DefaultHeaders),
        };
        Webhooks = new WebhooksResource();
        InitResources(Core);
    }

    /// <summary>Creates a client with a secret key and default settings.</summary>
    /// <param name="apiKey">Secret key (<c>sk_live_...</c> or <c>sk_test_...</c>).</param>
    public DesktopAccountingApiClient(string apiKey)
        : this(new ClientOptions { ApiKey = apiKey })
    {
    }

    private DesktopAccountingApiClient(ApiCore core)
    {
        Core = core;
        Webhooks = new WebhooksResource();
        InitResources(core);
    }

    internal ApiCore Core { get; }

    /// <summary>Webhook signature verification (Standard Webhooks). Also available without a client through <see cref="WebhookVerifier"/>.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>The default end user of this client, or <c>null</c>.</summary>
    public string? EndUserId => Core.EndUserId;

    /// <summary>The API base URL this client sends requests to.</summary>
    public string BaseUrl => Core.BaseUrl;

    /// <summary>Returns a client whose QuickBooks Desktop calls default to <paramref name="endUserId"/>. It shares this client's HTTP connections and settings; disposing it does nothing.</summary>
    /// <param name="endUserId">The end user (<c>eu_...</c>).</param>
    /// <returns>A client bound to the end user.</returns>
    public DesktopAccountingApiClient ForEndUser(string endUserId)
    {
        if (string.IsNullOrEmpty(endUserId)) throw new ArgumentException("endUserId must be a non-empty string.", nameof(endUserId));
        return new DesktopAccountingApiClient(Core.WithEndUser(endUserId));
    }

    /// <summary>Removes trailing slashes and one trailing <c>/v1</c>: the SDK adds <c>/v1/...</c> itself.</summary>
    private static string NormalizeBaseUrl(string url)
    {
        var trimmed = url.TrimEnd('/');
        return trimmed.EndsWith("/v1", StringComparison.Ordinal) ? trimmed.Substring(0, trimmed.Length - 3) : trimmed;
    }

    /// <summary>Disposes the HTTP client the SDK created. A client you passed in <see cref="ClientOptions.HttpClient"/> is left open.</summary>
    public void Dispose() => _ownedHttpClient?.Dispose();

#if NET8_0_OR_GREATER
    private static SocketsHttpHandler CreateHandler()
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        };
    }
#else
    private static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        };
    }
#endif
}
