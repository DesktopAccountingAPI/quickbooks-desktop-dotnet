using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Xunit;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Conformance;

/// <summary>
/// The cross-language conformance suite (conformance/README.md): every scenario in
/// fixtures/scenarios.json runs through the real SDK HTTP stack against mock-server.mjs, then
/// the mock server's verify endpoint and the scenario outcome are checked. Also runs every
/// webhook and API-key vector.
/// </summary>
public sealed class ConformanceTests : IClassFixture<MockServer>
{
    private static readonly Dictionary<string, Type> s_classes = new()
    {
        ["DaapiError"] = typeof(DaapiException),
        ["ApiError"] = typeof(ApiException),
        ["InvalidRequestError"] = typeof(InvalidRequestException),
        ["AuthenticationError"] = typeof(AuthenticationException),
        ["PermissionError"] = typeof(PermissionException),
        ["BillingError"] = typeof(BillingException),
        ["RateLimitError"] = typeof(RateLimitException),
        ["IntegrationConnectionError"] = typeof(IntegrationConnectionException),
        ["IntegrationError"] = typeof(IntegrationException),
        ["OutcomeUnknownError"] = typeof(OutcomeUnknownException),
        ["InternalError"] = typeof(InternalException),
        ["CursorExpiredError"] = typeof(CursorExpiredException),
        ["RequestPendingError"] = typeof(RequestPendingException),
        ["ApiConnectionError"] = typeof(ApiConnectionException),
    };

    private static readonly JsonSerializerOptions s_camel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly MockServer _server;

    public ConformanceTests(MockServer server)
    {
        _server = server;
    }

    public static IEnumerable<object[]> ScenarioNames() =>
        Fixtures.Scenarios.GetProperty("scenarios").EnumerateArray().Select(s => new object[] { s.GetProperty("name").GetString()! });

    public static IEnumerable<object[]> WebhookCases() =>
        Fixtures.Webhooks.GetProperty("cases").EnumerateArray().Select(c => new object[] { c.GetProperty("name").GetString()! });

    public static IEnumerable<object[]> ValidKeys() =>
        Fixtures.ApiKeys.GetProperty("valid").EnumerateArray().Select(k => new object[] { k.GetString()! });

    public static IEnumerable<object[]> InvalidKeys() =>
        Fixtures.ApiKeys.GetProperty("invalid").EnumerateArray().Select(k => new object[] { k.GetProperty("key").GetString()!, k.GetProperty("reason").GetString()! });

    private sealed class Observed
    {
        public object? Result;
        public Exception? Error;
        public List<string?> Items = new();
        public object? Page;
        public (string Id, string Status)? Handle;
        public (int Status, string? RequestId, IReadOnlyDictionary<string, string> Headers)? Response;
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task Scenario(string name)
    {
        var root = Fixtures.Scenarios;
        var sc = root.GetProperty("scenarios").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        await _server.ResetAsync(name);

        var observed = new Observed();
        DesktopAccountingApiClient? client = null;
        try
        {
            client = BuildClient(root, sc, _server.Url + "/s/" + name);
        }
        catch (DaapiException ex)
        {
            observed.Error = ex;
        }
        if (client is not null)
        {
            using (client)
            {
                try
                {
                    await RunCall(client, sc.GetProperty("call"), observed);
                }
                catch (DaapiException ex)
                {
                    observed.Error = ex;
                }
            }
        }

        var verify = await _server.VerifyAsync(name);
        Assert.True(verify.GetProperty("ok").GetBoolean(), $"{name}: mock server verification failed: {verify.GetProperty("errors")}");
        CheckOutcome(name, sc.GetProperty("outcome"), observed);
    }

    private static DesktopAccountingApiClient BuildClient(JsonElement root, JsonElement sc, string baseUrl)
    {
        var defaults = root.GetProperty("defaultClient");
        var overrides = sc.TryGetProperty("client", out var c) ? c : default;
        JsonElement Pick(string key) =>
            overrides.ValueKind == JsonValueKind.Object && overrides.TryGetProperty(key, out var v) ? v : defaults.TryGetProperty(key, out var d) ? d : default;
        var apiKey = overrides.ValueKind == JsonValueKind.Object && overrides.TryGetProperty("apiKey", out var k) ? k.GetString() : root.GetProperty("apiKey").GetString();
        var endUser = Pick("endUserId");
        var maxRetries = Pick("maxRetries");
        var timeoutMs = Pick("timeoutMs");
        return new DesktopAccountingApiClient(new ClientOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            EndUserId = endUser.ValueKind == JsonValueKind.String ? endUser.GetString() : null,
            MaxRetries = maxRetries.ValueKind == JsonValueKind.Number ? maxRetries.GetInt32() : 2,
            Timeout = timeoutMs.ValueKind == JsonValueKind.Number ? TimeSpan.FromMilliseconds(timeoutMs.GetInt32()) : TimeSpan.FromSeconds(100),
        });
    }

    private static T? Params<T>(JsonElement call) where T : class =>
        call.TryGetProperty("params", out var p) ? JsonSerializer.Deserialize<T>(p.GetRawText(), DesktopAccountingApiJson.Options) : null;

    private static string PathParam(JsonElement call, string name) => call.GetProperty("path").GetProperty(name).GetString()!;

    private static RequestOptions? Options(JsonElement call)
    {
        if (!call.TryGetProperty("options", out var o)) return null;
        var options = new RequestOptions();
        if (o.TryGetProperty("idempotencyKey", out var key)) options.IdempotencyKey = key.GetString();
        if (o.TryGetProperty("endUserId", out var eu)) options.EndUserId = eu.GetString();
        if (o.TryGetProperty("timeoutMs", out var t)) options.Timeout = TimeSpan.FromMilliseconds(t.GetInt32());
        if (o.TryGetProperty("serverTimeoutSeconds", out var st)) options.ServerTimeout = TimeSpan.FromSeconds(st.GetInt32());
        if (o.TryGetProperty("maxRetries", out var mr)) options.MaxRetries = mr.GetInt32();
        return options;
    }

    private static async Task RunCall(DesktopAccountingApiClient client, JsonElement call, Observed observed)
    {
        var op = call.GetProperty("op").GetString();
        var kind = call.GetProperty("kind").GetString();
        var options = Options(call);
        switch (op, kind)
        {
            case ("qbd.invoices.list", "iterate" or "firstPage"):
                await RunPager(client.Qbd.Invoices.ListAsync(Params<InvoiceListParams>(call), options), kind, observed);
                break;
            case ("endUsers.list", "iterate" or "firstPage"):
                await RunPager(client.EndUsers.ListAsync(Params<EndUserListParams>(call), options), kind, observed);
                break;
            case ("qbd.invoices.create", "call"):
                observed.Result = await client.Qbd.Invoices.CreateAsync(Params<InvoiceCreateInput>(call)!, options);
                break;
            case ("qbd.invoices.create", "enqueue"):
                {
                    var handle = await client.Qbd.Invoices.Enqueue.CreateAsync(Params<InvoiceCreateInput>(call)!, options);
                    observed.Handle = (handle.Id, handle.Request.Status);
                    var wait = call.GetProperty("wait").GetProperty("timeoutMs").GetInt32();
                    observed.Result = await handle.WaitAsync(TimeSpan.FromMilliseconds(wait));
                    break;
                }
            case ("qbd.invoices.update", "call"):
                observed.Result = await client.Qbd.Invoices.UpdateAsync(PathParam(call, "id"), Params<InvoiceUpdateInput>(call)!, options);
                break;
            case ("qbd.invoices.void", "call"):
                observed.Result = await client.Qbd.Invoices.VoidAsync(PathParam(call, "id"), options);
                break;
            case ("qbd.customers.retrieve", "call"):
                observed.Result = await client.Qbd.Customers.RetrieveAsync(PathParam(call, "id"), options);
                break;
            case ("qbd.customers.retrieve", "withResponse"):
                {
                    var response = await client.Qbd.Customers.RetrieveWithResponseAsync(PathParam(call, "id"), options);
                    observed.Result = response.Data;
                    observed.Response = (response.StatusCode, response.RequestId, response.Headers);
                    break;
                }
            case ("qbd.healthCheck", "call"):
                observed.Result = await client.Qbd.HealthCheckAsync(options);
                break;
            case ("endUsers.passthrough", "call"):
                observed.Result = await client.EndUsers.PassthroughAsync(PathParam(call, "id"), Params<PassthroughInput>(call), options);
                break;
            case ("requests.retrieve", "call"):
                observed.Result = await client.Requests.RetrieveAsync(PathParam(call, "id"), null, options);
                break;
            default:
                throw new InvalidOperationException($"The .NET conformance runner does not map {op} / {kind}.");
        }
    }

    private static async Task RunPager<T>(Pager<T> pager, string? kind, Observed observed)
    {
        if (kind == "firstPage")
        {
            var page = await pager.GetFirstPageAsync();
            observed.Page = new
            {
                ids = page.Data.Select(IdOf).ToList(),
                page.NextCursor,
                page.HasMore,
                page.RemainingCount,
            };
            return;
        }
        await foreach (var item in pager) observed.Items.Add(IdOf(item));
    }

    private static string? IdOf<T>(T item)
    {
        var json = JsonSerializer.SerializeToElement(item, DesktopAccountingApiJson.Options);
        return json.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    // ------------------------------------------------------------- outcome

    private static void CheckOutcome(string name, JsonElement outcome, Observed observed)
    {
        if (!outcome.TryGetProperty("error", out var expectedError) && observed.Error is not null)
        {
            throw new Xunit.Sdk.XunitException($"{name}: unexpected {observed.Error.GetType().Name}: {observed.Error.Message}");
        }
        if (outcome.TryGetProperty("result", out var result))
        {
            Assert.True(observed.Result is not null, $"{name}: no result");
            var actual = JsonSerializer.SerializeToElement(observed.Result, observed.Result!.GetType(), DesktopAccountingApiJson.Options);
            foreach (var p in result.EnumerateObject())
            {
                var value = Navigate(actual, p.Name);
                Assert.True(value is { } v && JsonEquals(v, p.Value), $"{name}: result.{p.Name} = {(value is { } x ? x.GetRawText() : "<missing>")}, expected {p.Value.GetRawText()}");
            }
        }
        if (outcome.TryGetProperty("items", out var items))
        {
            Assert.Equal(items.EnumerateArray().Select(i => i.GetString()).ToList(), observed.Items);
        }
        if (outcome.TryGetProperty("page", out var page))
        {
            Assert.NotNull(observed.Page);
            var actual = JsonSerializer.SerializeToElement(observed.Page, s_camel);
            foreach (var p in page.EnumerateObject())
            {
                Assert.True(JsonEquals(actual.GetProperty(p.Name), p.Value), $"{name}: page.{p.Name} = {actual.GetProperty(p.Name).GetRawText()}, expected {p.Value.GetRawText()}");
            }
        }
        if (outcome.TryGetProperty("handle", out var handle))
        {
            Assert.NotNull(observed.Handle);
            Assert.Equal(handle.GetProperty("id").GetString(), observed.Handle!.Value.Id);
            Assert.Equal(handle.GetProperty("status").GetString(), observed.Handle!.Value.Status);
        }
        if (outcome.TryGetProperty("response", out var response))
        {
            Assert.NotNull(observed.Response);
            var r = observed.Response!.Value;
            if (response.TryGetProperty("status", out var st)) Assert.Equal(st.GetInt32(), r.Status);
            if (response.TryGetProperty("requestId", out var rid)) Assert.Equal(rid.GetString(), r.RequestId);
            if (response.TryGetProperty("headers", out var headers))
            {
                foreach (var h in headers.EnumerateObject())
                {
                    Assert.True(r.Headers.TryGetValue(h.Name, out var v) && v == h.Value.GetString(), $"{name}: header {h.Name}");
                }
            }
        }
        if (expectedError.ValueKind == JsonValueKind.Object) CheckError(name, expectedError, observed.Error);
    }

    private static void CheckError(string name, JsonElement expected, Exception? error)
    {
        Assert.True(error is not null, $"{name}: expected an error, the call succeeded");
        var canonical = expected.GetProperty("class").GetString()!;
        var type = s_classes[canonical];
        if (canonical == "ApiError") Assert.True(error!.GetType() == typeof(ApiException), $"{name}: expected exactly ApiException, got {error.GetType().Name}");
        else Assert.True(type.IsInstanceOfType(error), $"{name}: expected {type.Name}, got {error!.GetType().Name}: {error.Message}");

        var actual = new JsonObject();
        if (error is ApiException api)
        {
            actual["type"] = api.Type;
            actual["code"] = api.Code;
            actual["status"] = api.Status;
            actual["message"] = api.Message;
            actual["userFacingMessage"] = api.UserFacingMessage;
            actual["requestId"] = api.RequestId;
            actual["param"] = api.Param;
            actual["retryable"] = api.Retryable;
            actual["outcome"] = api.Outcome;
            actual["integrationCode"] = api.IntegrationCode;
            actual["cause"] = api.Cause;
            actual["docsUrl"] = api.DocsUrl;
            actual["fixes"] = JsonSerializer.SerializeToNode(api.Fixes, DesktopAccountingApiJson.Options);
            actual["details"] = JsonSerializer.SerializeToNode(api.Details, DesktopAccountingApiJson.Options);
        }
        if (error is CursorExpiredException cursor)
        {
            actual["itemsYielded"] = cursor.ItemsYielded;
            actual["pagesServed"] = cursor.PagesServed;
            actual["lastId"] = cursor.LastId;
            actual["lastUpdatedAt"] = cursor.LastUpdatedAt;
        }
        if (error is RequestPendingException pending) actual["requestId"] = pending.RequestId;
        var actualElement = JsonSerializer.SerializeToElement(actual);

        foreach (var p in expected.EnumerateObject())
        {
            if (p.Name == "class") continue;
            Assert.True(actualElement.TryGetProperty(p.Name, out var value), $"{name}: error field {p.Name} is not exposed by {error!.GetType().Name}");
            if (p.Name == "details")
            {
                foreach (var d in p.Value.EnumerateObject())
                {
                    Assert.True(value.TryGetProperty(d.Name, out var dv) && JsonEquals(dv, d.Value), $"{name}: error.details.{d.Name} = {value.GetRawText()}, expected {d.Value.GetRawText()}");
                }
                continue;
            }
            Assert.True(JsonEquals(value, p.Value), $"{name}: error.{p.Name} = {value.GetRawText()}, expected {p.Value.GetRawText()}");
        }
    }

    private static JsonElement? Navigate(JsonElement element, string dotted)
    {
        var current = element;
        foreach (var part in dotted.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out var next)) return null;
            current = next;
        }
        return current;
    }

    private static bool JsonEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
        {
            return (a.ValueKind is JsonValueKind.True or JsonValueKind.False) && (b.ValueKind is JsonValueKind.True or JsonValueKind.False) && a.GetBoolean() == b.GetBoolean();
        }
        switch (a.ValueKind)
        {
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
            case JsonValueKind.Number:
                return a.GetDecimal() == b.GetDecimal();
            case JsonValueKind.Array:
                var xs = a.EnumerateArray().ToList();
                var ys = b.EnumerateArray().ToList();
                return xs.Count == ys.Count && xs.Zip(ys).All(t => JsonEquals(t.First, t.Second));
            case JsonValueKind.Object:
                var pa = a.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var pb = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                return pa.Count == pb.Count && pa.All(kv => pb.TryGetValue(kv.Key, out var v) && JsonEquals(kv.Value, v));
            default:
                return true;
        }
    }

    // ------------------------------------------------------------ webhooks

    [Theory]
    [MemberData(nameof(WebhookCases))]
    public void WebhookVector(string name)
    {
        var c = Fixtures.Webhooks.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
        var body = c.GetProperty("body").GetString()!;
        var headers = c.GetProperty("headers").EnumerateObject().Select(h => new KeyValuePair<string, string>(h.Name, h.Value.GetString()!)).ToList();
        var secret = c.GetProperty("secret").GetString()!;
        var options = new WebhookVerifyOptions { Clock = () => DateTimeOffset.FromUnixTimeSeconds(c.GetProperty("now").GetInt64()) };
        var signatureOnly = c.TryGetProperty("signatureOnly", out var so) && so.GetBoolean();
        if (!c.GetProperty("valid").GetBoolean())
        {
            Assert.Throws<WebhookVerificationException>(() =>
            {
                if (signatureOnly) WebhookVerifier.VerifySignature(body, headers, secret, options);
                else WebhookVerifier.Verify(body, headers, secret, options);
            });
            return;
        }
        if (signatureOnly)
        {
            WebhookVerifier.VerifySignature(body, headers, secret, options);
            return;
        }
        var ev = WebhookVerifier.Verify(body, headers, secret, options);
        var expected = c.GetProperty("event");
        Assert.Equal(expected.GetProperty("id").GetString(), ev.Id);
        Assert.Equal(expected.GetProperty("type").GetString(), ev.Type);
        Assert.Equal(DateTimeOffset.Parse(expected.GetProperty("timestamp").GetString()!, CultureInfo.InvariantCulture), ev.Timestamp);
        Assert.Equal(expected.GetProperty("projectId").GetString(), ev.ProjectId);
        Assert.Equal(expected.GetProperty("dataStatus").GetString(), ev.Data.GetProperty("status").GetString());
        Assert.Equal(expected.GetProperty("dataId").GetString(), ev.Data.GetProperty("id").GetString());
    }

    // ------------------------------------------------------------ API keys

    [Theory]
    [MemberData(nameof(ValidKeys))]
    public void ApiKeyValid(string key)
    {
        using var client = new DesktopAccountingApiClient(new ClientOptions { ApiKey = key, BaseUrl = "http://127.0.0.1:9" });
        Assert.NotNull(client.Qbd);
    }

    [Theory]
    [MemberData(nameof(InvalidKeys))]
    public void ApiKeyInvalid(string key, string reason)
    {
        var ex = Assert.ThrowsAny<DaapiException>(() => new DesktopAccountingApiClient(new ClientOptions { ApiKey = key, BaseUrl = "http://127.0.0.1:9" }));
        Assert.False(ex is ApiException, reason);
    }
}
