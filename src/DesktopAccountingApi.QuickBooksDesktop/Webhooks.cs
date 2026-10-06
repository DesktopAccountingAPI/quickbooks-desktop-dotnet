using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>Known webhook event types. <see cref="WebhookEvent.Type"/> is an open string: new types can appear.</summary>
public static class WebhookEventTypes
{
    /// <summary>A request reached <c>succeeded</c>.</summary>
    public const string RequestSucceeded = "request.succeeded";

    /// <summary>A request reached <c>failed</c>.</summary>
    public const string RequestFailed = "request.failed";

    /// <summary>An async request was canceled.</summary>
    public const string RequestCanceled = "request.canceled";

    /// <summary>A write became <c>outcome_unknown</c>.</summary>
    public const string RequestOutcomeUnknown = "request.outcome_unknown";

    /// <summary>An <c>outcome_unknown</c> write was resolved by recovery.</summary>
    public const string RequestOutcomeResolved = "request.outcome_resolved";

    /// <summary>An auth session finished and the first health check passed.</summary>
    public const string ConnectionSetupCompleted = "connection.setup_completed";

    /// <summary>A connection's derived status changed.</summary>
    public const string ConnectionStatusChanged = "connection.status_changed";

    /// <summary>A test event sent from the dashboard or <c>POST /v1/webhook-endpoints/{id}/test</c>.</summary>
    public const string WebhookTest = "webhook.test";
}

/// <summary>A verified webhook event.</summary>
public sealed class WebhookEvent
{
    internal WebhookEvent(string id, string type, DateTimeOffset timestamp, string? projectId, JsonElement data, JsonElement raw)
    {
        Id = id;
        Type = type;
        Timestamp = timestamp;
        ProjectId = projectId;
        Data = data;
        Raw = raw;
    }

    /// <summary>Event ID (<c>evt_...</c>), equal to the <c>webhook-id</c> header. Deduplicate on it: delivery is at least once.</summary>
    public string Id { get; }

    /// <summary>Event type, for example <c>request.succeeded</c>. See <see cref="WebhookEventTypes"/>.</summary>
    public string Type { get; }

    /// <summary>When the event happened.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>The project (<c>proj_...</c>) the event belongs to.</summary>
    public string? ProjectId { get; }

    /// <summary>Event data. For request events: the request resource without <c>result</c> and <c>timeline</c>.</summary>
    public JsonElement Data { get; }

    /// <summary>The whole event payload.</summary>
    public JsonElement Raw { get; }

    /// <summary>Deserializes <see cref="Data"/> into an SDK model, for example <see cref="Models.Request"/>.</summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <returns>The data as <typeparamref name="T"/>.</returns>
    public T? DataAs<T>() => Data.Deserialize<T>(DesktopAccountingApiJson.Options);
}

/// <summary>Options for webhook verification.</summary>
public sealed class WebhookVerifyOptions
{
    /// <summary>Maximum difference between <c>webhook-timestamp</c> and the clock, in both directions. Default 5 minutes.</summary>
    public TimeSpan Tolerance { get; set; } = TimeSpan.FromSeconds(300);

    /// <summary>Clock used for the timestamp check. Default <see cref="DateTimeOffset.UtcNow"/>.</summary>
    public Func<DateTimeOffset>? Clock { get; set; }
}

/// <summary>
/// Verifies Standard Webhooks signatures: HMAC-SHA256 over <c>{webhook-id}.{webhook-timestamp}.{body}</c>,
/// keyed with the endpoint secret (<c>whsec_</c> + base64). The <c>webhook-signature</c> header may hold
/// several space-separated <c>v1,&lt;base64&gt;</c> entries during secret rotation. Works without an API key.
/// </summary>
public static class WebhookVerifier
{
    /// <summary>Verifies the signature and timestamp, then parses the event.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="headers">Request headers (names are matched case-insensitively).</param>
    /// <param name="secret">The endpoint's signing secret, with or without the <c>whsec_</c> prefix.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <returns>The event.</returns>
    /// <exception cref="WebhookVerificationException">Verification failed.</exception>
    public static WebhookEvent Verify(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, WebhookVerifyOptions? options = null) =>
        Verify(payload, Lookup(headers), secret, options);

    /// <summary>Verifies the signature and timestamp, then parses the event.</summary>
    /// <param name="payload">The raw request body, exactly as received.</param>
    /// <param name="header">Returns a request header's value by name, or <c>null</c> (for example <c>name =&gt; request.Headers[name]</c>).</param>
    /// <param name="secret">The endpoint's signing secret, with or without the <c>whsec_</c> prefix.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <returns>The event.</returns>
    /// <exception cref="WebhookVerificationException">Verification failed.</exception>
    public static WebhookEvent Verify(string payload, Func<string, string?> header, string secret, WebhookVerifyOptions? options = null)
    {
        VerifySignature(payload, header, secret, options);
        return Parse(payload);
    }

    /// <summary>Checks only the signature and timestamp; does not parse the body.</summary>
    /// <param name="payload">The raw request body.</param>
    /// <param name="headers">Request headers.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookVerificationException">Verification failed.</exception>
    public static void VerifySignature(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, WebhookVerifyOptions? options = null) =>
        VerifySignature(payload, Lookup(headers), secret, options);

    /// <summary>Checks only the signature and timestamp; does not parse the body.</summary>
    /// <param name="payload">The raw request body.</param>
    /// <param name="header">Returns a request header's value by name, or <c>null</c>.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookVerificationException">Verification failed.</exception>
    public static void VerifySignature(string payload, Func<string, string?> header, string secret, WebhookVerifyOptions? options = null)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (header is null) throw new ArgumentNullException(nameof(header));
        var id = header("webhook-id");
        var timestamp = header("webhook-timestamp");
        var signatures = header("webhook-signature");
        if (string.IsNullOrEmpty(id)) throw new WebhookVerificationException("Missing webhook-id header.");
        if (string.IsNullOrEmpty(timestamp)) throw new WebhookVerificationException("Missing webhook-timestamp header.");
        if (string.IsNullOrEmpty(signatures)) throw new WebhookVerificationException("Missing webhook-signature header.");
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)) throw new WebhookVerificationException("Invalid webhook-timestamp header.");
        var now = (options?.Clock ?? (() => DateTimeOffset.UtcNow))().ToUnixTimeSeconds();
        var tolerance = (long)(options?.Tolerance ?? TimeSpan.FromSeconds(300)).TotalSeconds;
        if (now - seconds > tolerance) throw new WebhookVerificationException("The webhook timestamp is too old.");
        if (seconds - now > tolerance) throw new WebhookVerificationException("The webhook timestamp is in the future.");

        var expected = Hmac(SecretBytes(secret), id + "." + timestamp + "." + payload);
        foreach (var entry in signatures!.Split(' '))
        {
            var comma = entry.IndexOf(',');
            if (comma <= 0 || entry.Substring(0, comma) != "v1") continue;
            byte[] candidate;
            try
            {
                candidate = Convert.FromBase64String(entry.Substring(comma + 1));
            }
            catch (FormatException)
            {
                continue;
            }
            if (FixedTimeEquals(candidate, expected)) return;
        }
        throw new WebhookVerificationException("No webhook-signature matches the payload and secret.");
    }

    /// <summary>Computes a <c>webhook-signature</c> value (<c>v1,&lt;base64&gt;</c>), for tests and local receivers.</summary>
    /// <param name="payload">The body.</param>
    /// <param name="webhookId">The <c>webhook-id</c>.</param>
    /// <param name="timestamp">The <c>webhook-timestamp</c>.</param>
    /// <param name="secret">The signing secret.</param>
    /// <returns>The signature header value.</returns>
    public static string Sign(string payload, string webhookId, DateTimeOffset timestamp, string secret) =>
        "v1," + Convert.ToBase64String(Hmac(SecretBytes(secret), webhookId + "." + timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + "." + payload));

    private static WebhookEvent Parse(string payload)
    {
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new WebhookVerificationException("The webhook body is not JSON: " + ex.Message);
        }
        if (root.ValueKind != JsonValueKind.Object) throw new WebhookVerificationException("The webhook body is not a JSON object.");
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var id = Str("id");
        var type = Str("type");
        var ts = Str("timestamp");
        if (id is null || type is null || ts is null || !DateTimeOffset.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            throw new WebhookVerificationException("The webhook body is not a Desktop Accounting API event (id, type and timestamp are required).");
        }
        var data = root.TryGetProperty("data", out var d) ? d : default;
        return new WebhookEvent(id, type, timestamp, Str("projectId"), data, root);
    }

    private static Func<string, string?> Lookup(IEnumerable<KeyValuePair<string, string>> headers)
    {
        if (headers is null) throw new ArgumentNullException(nameof(headers));
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers) map[h.Key] = h.Value;
        return name => map.TryGetValue(name, out var v) ? v : null;
    }

    private static byte[] SecretBytes(string secret)
    {
        if (string.IsNullOrEmpty(secret)) throw new WebhookVerificationException("The webhook secret is empty.");
        var encoded = secret.StartsWith("whsec_", StringComparison.Ordinal) ? secret.Substring("whsec_".Length) : secret;
        try
        {
            return Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw new WebhookVerificationException("The webhook secret is not valid base64 after the whsec_ prefix.");
        }
    }

    private static byte[] Hmac(byte[] key, string message)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}

/// <summary>Webhook helpers on the client (<c>client.Webhooks</c>). Same as <see cref="WebhookVerifier"/>.</summary>
public sealed class WebhooksResource
{
    internal WebhooksResource()
    {
    }

    /// <inheritdoc cref="WebhookVerifier.Verify(string, IEnumerable{KeyValuePair{string, string}}, string, WebhookVerifyOptions?)"/>
#pragma warning disable CA1822 // Instance members keep the client.Webhooks.Verify(...) call shape.
    public WebhookEvent Verify(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, WebhookVerifyOptions? options = null) =>
        WebhookVerifier.Verify(payload, headers, secret, options);

    /// <inheritdoc cref="WebhookVerifier.Verify(string, Func{string, string?}, string, WebhookVerifyOptions?)"/>
    public WebhookEvent Verify(string payload, Func<string, string?> header, string secret, WebhookVerifyOptions? options = null) =>
        WebhookVerifier.Verify(payload, header, secret, options);

    /// <inheritdoc cref="WebhookVerifier.VerifySignature(string, IEnumerable{KeyValuePair{string, string}}, string, WebhookVerifyOptions?)"/>
    public void VerifySignature(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, WebhookVerifyOptions? options = null) =>
        WebhookVerifier.VerifySignature(payload, headers, secret, options);
#pragma warning restore CA1822
}
