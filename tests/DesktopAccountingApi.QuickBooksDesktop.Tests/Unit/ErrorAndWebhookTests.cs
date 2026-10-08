using System;
using System.Collections.Generic;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Xunit;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Unit;

public class ErrorMappingTests
{
    private static readonly Dictionary<string, string> s_headers = new(StringComparer.OrdinalIgnoreCase) { ["Daapi-Request-Id"] = "req_header" };

    [Theory]
    [InlineData("INVALID_REQUEST_ERROR", "INVALID_PARAMETER", typeof(InvalidRequestException))]
    [InlineData("INVALID_REQUEST_ERROR", "CURSOR_EXPIRED", typeof(CursorExpiredException))]
    [InlineData("AUTHENTICATION_ERROR", "API_KEY_INVALID", typeof(AuthenticationException))]
    [InlineData("PERMISSION_ERROR", "PERMISSION_DENIED", typeof(PermissionException))]
    [InlineData("BILLING_ERROR", "BILLING_REQUIRED", typeof(BillingException))]
    [InlineData("RATE_LIMIT_ERROR", "RATE_LIMITED", typeof(RateLimitException))]
    [InlineData("INTEGRATION_CONNECTION_ERROR", "QBD_MODAL_DIALOG_OPEN", typeof(IntegrationConnectionException))]
    [InlineData("INTEGRATION_ERROR", "QBD_OBJECT_NOT_FOUND", typeof(IntegrationException))]
    [InlineData("OUTCOME_UNKNOWN_ERROR", "QBD_WRITE_OUTCOME_UNKNOWN", typeof(OutcomeUnknownException))]
    [InlineData("INTERNAL_ERROR", "INTERNAL_ERROR", typeof(InternalException))]
    [InlineData("FUTURE_ERROR_TYPE", "FUTURE_CODE", typeof(ApiException))]
    public void Error_type_selects_the_exception_class(string type, string code, Type expected)
    {
        var body = $"{{\"error\":{{\"type\":\"{type}\",\"code\":\"{code}\",\"message\":\"m\",\"userFacingMessage\":\"u\",\"fixes\":[{{\"actor\":\"developer\",\"action\":\"a\"}}],\"retryable\":false,\"outcome\":\"not_applied\",\"details\":{{\"k\":1}}}}}}";
        var ex = ErrorFactory.FromResponse(400, s_headers, body);
        Assert.Equal(expected, ex.GetType());
        Assert.Equal(code, ex.Code);
        Assert.Equal("m", ex.Message);
        Assert.Equal("u", ex.UserFacingMessage);
        Assert.Equal("developer", ex.Fixes[0].Actor);
        Assert.Equal("req_header", ex.RequestId);
        Assert.Equal(1, ex.Details["k"].GetInt32());
        Assert.Equal(400, ex.Status);
        // Unhandled-exception output and loggers use ToString(): it carries the code and request ID.
        Assert.StartsWith($"{expected.FullName}: 400 {code} m (req_header)", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Non_json_error_is_the_base_class_with_the_status()
    {
        var ex = ErrorFactory.FromResponse(502, s_headers, "<html>Bad gateway</html>");
        Assert.Equal(typeof(ApiException), ex.GetType());
        Assert.Equal(502, ex.Status);
        Assert.Null(ex.Code);
        Assert.Empty(ex.Fixes);
        Assert.Empty(ex.Details);
    }

    [Fact]
    public void Error_code_constants_come_from_the_contract()
    {
        Assert.Equal("QBD_MODAL_DIALOG_OPEN", ErrorCodes.QbdModalDialogOpen);
        Assert.Equal("INTEGRATION_CONNECTION_ERROR", ErrorTypes.IntegrationConnectionError);
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("2", 2.0)]
    [InlineData("1.5", 1.5)]
    public void Retry_after_seconds(string value, double seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), ApiCore.RetryAfter(value));
    }

    [Fact]
    public void Retry_after_http_date_and_garbage()
    {
        var future = DateTimeOffset.UtcNow.AddSeconds(30).ToString("r", System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(ApiCore.RetryAfter(future)!.Value.TotalSeconds, 25, 31);
        Assert.Null(ApiCore.RetryAfter("soon"));
        Assert.Null(ApiCore.RetryAfter(null));
        Assert.Null(ApiCore.RetryAfter("-1"));
    }

    [Fact]
    public void Backoff_doubles_with_jitter_and_caps_at_eight_seconds()
    {
        using var client = new StubHandler().Client();
        client.Core.Jitter = () => 0.999999;
        Assert.InRange(client.Core.Backoff(0).TotalSeconds, 0.49, 0.5);
        Assert.InRange(client.Core.Backoff(2).TotalSeconds, 1.99, 2.0);
        Assert.InRange(client.Core.Backoff(10).TotalSeconds, 7.99, 8.0);
        client.Core.Jitter = () => 0;
        Assert.Equal(TimeSpan.FromSeconds(0.25), client.Core.Backoff(0));
    }
}

public class WebhookTests
{
    private const string Secret = "whsec_Y29uZm9ybWFuY2Utd2ViaG9vay1zZWNyZXQtMzJieXQ=";
    private const string Body = "{\"id\":\"evt_1\",\"type\":\"request.succeeded\",\"timestamp\":\"2026-10-05T16:04:01.311Z\",\"projectId\":\"proj_1\",\"data\":{\"objectType\":\"request\",\"id\":\"req_1\",\"status\":\"succeeded\"}}";
    private static readonly DateTimeOffset s_now = DateTimeOffset.FromUnixTimeSeconds(1791216241);

    private static Dictionary<string, string> Headers(string signature, DateTimeOffset at) => new()
    {
        ["Webhook-Id"] = "evt_1",
        ["Webhook-Timestamp"] = at.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["Webhook-Signature"] = signature,
    };

    private static WebhookVerifyOptions At(DateTimeOffset now) => new() { Clock = () => now };

    [Fact]
    public void Signs_and_verifies_with_and_without_prefix()
    {
        var signature = WebhookVerifier.Sign(Body, "evt_1", s_now, Secret);
        var ev = WebhookVerifier.Verify(Body, Headers(signature, s_now), Secret, At(s_now));
        Assert.Equal("evt_1", ev.Id);
        Assert.Equal(WebhookEventTypes.RequestSucceeded, ev.Type);
        Assert.Equal("succeeded", ev.DataAs<Request>()!.Status);
        WebhookVerifier.VerifySignature(Body, Headers(signature, s_now), Secret.Substring("whsec_".Length), At(s_now));
    }

    [Fact]
    public void Accepts_any_matching_signature_and_ignores_other_versions()
    {
        var good = WebhookVerifier.Sign(Body, "evt_1", s_now, Secret);
        var headers = Headers("v2,abc v1,AAAA " + good, s_now);
        WebhookVerifier.VerifySignature(Body, headers, Secret, At(s_now));
        Assert.Throws<WebhookVerificationException>(() => WebhookVerifier.VerifySignature(Body, Headers("v2," + good.Substring(3), s_now), Secret, At(s_now)));
    }

    [Fact]
    public void Header_lookup_is_case_insensitive_and_works_with_a_function()
    {
        var signature = WebhookVerifier.Sign(Body, "evt_1", s_now, Secret);
        var lower = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["webhook-id"] = "evt_1",
            ["webhook-timestamp"] = s_now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["webhook-signature"] = signature,
        };
        using var client = new StubHandler().Client();
        Assert.Equal("evt_1", client.Webhooks.Verify(Body, name => lower.TryGetValue(name, out var v) ? v : null, Secret, At(s_now)).Id);
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(301, false)]
    [InlineData(-300, true)]
    [InlineData(-301, false)]
    public void Tolerance_applies_in_both_directions(int skewSeconds, bool valid)
    {
        var sentAt = s_now.AddSeconds(-skewSeconds);
        var headers = Headers(WebhookVerifier.Sign(Body, "evt_1", sentAt, Secret), sentAt);
        if (valid) WebhookVerifier.VerifySignature(Body, headers, Secret, At(s_now));
        else Assert.Throws<WebhookVerificationException>(() => WebhookVerifier.VerifySignature(Body, headers, Secret, At(s_now)));
    }

    [Fact]
    public void Tampered_body_or_wrong_secret_fails()
    {
        var headers = Headers(WebhookVerifier.Sign(Body, "evt_1", s_now, Secret), s_now);
        Assert.Throws<WebhookVerificationException>(() => WebhookVerifier.Verify(Body.Replace("succeeded", "failed", StringComparison.Ordinal), headers, Secret, At(s_now)));
        Assert.Throws<WebhookVerificationException>(() => WebhookVerifier.Verify(Body, headers, "whsec_" + Convert.ToBase64String(new byte[32]), At(s_now)));
    }
}
