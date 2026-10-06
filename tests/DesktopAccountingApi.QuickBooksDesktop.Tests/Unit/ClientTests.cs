using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Xunit;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Unit;

public class ClientTests
{
    private const string Invoice = "{\"id\":\"7-1700000000\",\"objectType\":\"qbd_invoice\",\"subtotal\":\"105.50\"}";

    private static InvoiceCreateInput NewInvoice() => new()
    {
        CustomerId = "80000001-1700000000",
        TransactionDate = new DateOnly(2026, 10, 5),
        Lines = new[] { new InvoiceLineCreateInput { ItemId = "80000005-1700000000", Quantity = 2, Rate = 52.75m } },
    };

    [Fact]
    public async Task Sends_auth_user_agent_end_user_and_idempotency_headers()
    {
        var stub = new StubHandler().Respond(201, Invoice, ("Daapi-Request-Id", "req_1"));
        using var client = stub.Client();
        var invoice = await client.Qbd.Invoices.CreateAsync(NewInvoice());
        Assert.Equal(105.50m, invoice.Subtotal);

        var (request, body) = stub.Requests.Single();
        Assert.Equal("https://api.example.test/base/v1/quickbooks-desktop/invoices", request.RequestUri!.ToString());
        Assert.Equal("Bearer " + StubHandler.Key, StubHandler.Header(request, "Authorization"));
        Assert.Equal("desktopaccountingapi-dotnet/" + DesktopAccountingApiClient.SdkVersion, StubHandler.Header(request, "User-Agent"));
        Assert.Equal("eu_test", StubHandler.Header(request, "Daapi-End-User-Id"));
        Assert.True(Guid.TryParse(StubHandler.Header(request, "Idempotency-Key"), out _));
        Assert.Null(StubHandler.Header(request, "Prefer"));
        Assert.Equal("{\"customerId\":\"80000001-1700000000\",\"transactionDate\":\"2026-10-05\",\"lines\":[{\"itemId\":\"80000005-1700000000\",\"quantity\":2,\"rate\":\"52.75\"}]}", body);
        Assert.DoesNotContain(request.Headers, h => h.Key.StartsWith("Daapi-", StringComparison.OrdinalIgnoreCase) && h.Key != "Daapi-End-User-Id");
    }

    [Fact]
    public async Task Retries_reuse_the_idempotency_key_and_honor_retry_after()
    {
        var delays = new List<TimeSpan>();
        var stub = new StubHandler()
            .Fail()
            .Respond(503, "{\"error\":{\"type\":\"INTEGRATION_CONNECTION_ERROR\",\"code\":\"QBD_MODAL_DIALOG_OPEN\",\"outcome\":\"not_applied\"}}", ("Daapi-Should-Retry", "true"), ("Retry-After", "3"))
            .Respond(201, Invoice);
        using var client = stub.Client(delays: delays);
        await client.Qbd.Invoices.CreateAsync(NewInvoice());
        var keys = stub.Requests.Select(r => StubHandler.Header(r.Request, "Idempotency-Key")).Distinct().ToList();
        Assert.Single(keys);
        Assert.Equal(3, stub.Requests.Count);
        Assert.InRange(delays[0].TotalSeconds, 0.25, 0.5);
        Assert.Equal(TimeSpan.FromSeconds(3), delays[1]);
    }

    [Fact]
    public async Task Caller_idempotency_key_is_used()
    {
        var stub = new StubHandler().Respond(201, Invoice);
        using var client = stub.Client();
        await client.Qbd.Invoices.CreateAsync(NewInvoice(), new RequestOptions { IdempotencyKey = "order-1" });
        Assert.Equal("order-1", StubHandler.Header(stub.Requests[0].Request, "Idempotency-Key"));
    }

    [Theory]
    [InlineData("false", 503)]
    [InlineData(null, 500)]
    [InlineData(null, 400)]
    public async Task Does_not_retry_without_permission(string? shouldRetry, int status)
    {
        var headers = shouldRetry is null ? Array.Empty<(string, string)>() : new[] { ("Daapi-Should-Retry", shouldRetry) };
        var stub = new StubHandler().Respond(status, "{\"error\":{\"type\":\"INTERNAL_ERROR\",\"code\":\"INTERNAL_ERROR\",\"outcome\":\"not_applied\"}}", headers);
        using var client = stub.Client();
        await Assert.ThrowsAsync<InternalException>(() => client.Qbd.Customers.RetrieveAsync("1"));
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task Rate_limits_are_retried()
    {
        var stub = new StubHandler()
            .Respond(429, "{\"error\":{\"type\":\"RATE_LIMIT_ERROR\",\"code\":\"RATE_LIMITED\",\"outcome\":\"not_applied\"}}", ("Retry-After", "0"))
            .Respond(200, "{\"id\":\"1\"}");
        using var client = stub.Client();
        var customer = await client.Qbd.Customers.RetrieveAsync("1");
        Assert.Equal("1", customer.Id);
        Assert.Null(StubHandler.Header(stub.Requests[0].Request, "Idempotency-Key"));
    }

    [Fact]
    public async Task Missing_end_user_fails_before_sending()
    {
        var stub = new StubHandler();
        using var client = stub.Client(endUserId: null);
        var ex = await Assert.ThrowsAsync<DaapiException>(() => client.Qbd.HealthCheckAsync());
        Assert.Contains("end user", ex.Message, StringComparison.Ordinal);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task ForEndUser_and_per_call_end_user()
    {
        var stub = new StubHandler()
            .Respond(200, "{\"status\":\"ok\",\"duration\":1}")
            .Respond(200, "{\"status\":\"ok\",\"duration\":1}")
            .Respond(200, "{\"objectType\":\"list\",\"data\":[],\"nextCursor\":null,\"hasMore\":false}");
        using var client = stub.Client(endUserId: null);
        await client.ForEndUser("eu_a").Qbd.HealthCheckAsync(new RequestOptions { ServerTimeout = TimeSpan.FromSeconds(30) });
        await client.ForEndUser("eu_a").Qbd.HealthCheckAsync(new RequestOptions { EndUserId = "eu_b" });
        await client.ForEndUser("eu_a").EndUsers.ListAsync().GetFirstPageAsync();
        Assert.Equal("eu_a", StubHandler.Header(stub.Requests[0].Request, "Daapi-End-User-Id"));
        Assert.Equal("30", StubHandler.Header(stub.Requests[0].Request, "Daapi-Timeout-Seconds"));
        Assert.Equal("eu_b", StubHandler.Header(stub.Requests[1].Request, "Daapi-End-User-Id"));
        Assert.Null(StubHandler.Header(stub.Requests[2].Request, "Daapi-End-User-Id"));
    }

    [Fact]
    public async Task Pager_reads_ahead_and_continues_with_cursor_and_limit_only()
    {
        var stub = new StubHandler()
            .Respond(200, "{\"data\":[{\"id\":\"1\"},{\"id\":\"2\"}],\"nextCursor\":\"c2\",\"hasMore\":true,\"remainingCount\":1}")
            .Respond(200, "{\"data\":[{\"id\":\"3\"}],\"nextCursor\":null,\"hasMore\":false}");
        using var client = stub.Client();
        var pager = client.Qbd.Invoices.ListAsync(new InvoiceListParams { Limit = 2, CustomerIds = new[] { "c" } });
        var seen = new List<string>();
        await foreach (var invoice in pager)
        {
            seen.Add(invoice.Id);
            // Read-ahead: page 2 was requested before the caller finished page 1.
            if (invoice.Id == "1") Assert.Equal(2, stub.Requests.Count);
        }
        Assert.Equal(new[] { "1", "2", "3" }, seen);
        Assert.Equal("https://api.example.test/base/v1/quickbooks-desktop/invoices?limit=2&customerIds=c", stub.Requests[0].Request.RequestUri!.ToString());
        Assert.Equal("https://api.example.test/base/v1/quickbooks-desktop/invoices?cursor=c2&limit=2", stub.Requests[1].Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task Cursor_expiry_reports_progress()
    {
        var stub = new StubHandler()
            .Respond(200, "{\"data\":[{\"id\":\"1\",\"updatedAt\":\"2026-10-05T09:12:03-07:00\"}],\"nextCursor\":\"c2\",\"hasMore\":true}")
            .Respond(410, "{\"error\":{\"type\":\"INVALID_REQUEST_ERROR\",\"code\":\"CURSOR_EXPIRED\",\"outcome\":\"not_applicable\",\"details\":{\"reason\":\"evicted\"}}}", ("Daapi-Should-Retry", "false"));
        using var client = stub.Client();
        var items = new List<string>();
        var ex = await Assert.ThrowsAsync<CursorExpiredException>(async () =>
        {
            await foreach (var invoice in client.Qbd.Invoices.ListAsync()) items.Add(invoice.Id);
        });
        Assert.Equal(new[] { "1" }, items);
        Assert.Equal(1, ex.ItemsYielded);
        Assert.Equal(1, ex.PagesServed);
        Assert.Equal("1", ex.LastId);
        Assert.Equal("2026-10-05T09:12:03-07:00", ex.LastUpdatedAt);
        Assert.Equal("evicted", ex.Reason);
        Assert.Equal(410, ex.Status);
    }

    [Fact]
    public async Task Enqueue_sends_prefer_and_queue_ttl_and_handle_resolves()
    {
        const string queued = "{\"id\":\"req_1\",\"objectType\":\"request\",\"status\":\"queued\",\"result\":null}";
        const string done = "{\"id\":\"req_1\",\"objectType\":\"request\",\"status\":\"succeeded\",\"result\":" + Invoice + "}";
        var stub = new StubHandler().Respond(202, queued).Respond(200, done).Respond(200, done);
        using var client = stub.Client();
        var handle = await client.Qbd.Invoices.Enqueue.CreateAsync(NewInvoice(), new RequestOptions { QueueTtl = TimeSpan.FromMinutes(10) });
        Assert.Equal("req_1", handle.Id);
        Assert.Equal("respond-async", StubHandler.Header(stub.Requests[0].Request, "Prefer"));
        Assert.Equal("600", StubHandler.Header(stub.Requests[0].Request, "Daapi-Queue-Ttl-Seconds"));
        var invoice = await handle.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal("7-1700000000", invoice.Id);
        Assert.Matches("waitSeconds=30$", stub.Requests[1].Request.RequestUri!.ToString());
        Assert.Equal("7-1700000000", (await handle.ResultAsync()).Id);
    }

    [Fact]
    public async Task Failed_request_raises_the_typed_error_from_the_request()
    {
        const string failed = "{\"id\":\"req_1\",\"status\":\"failed\",\"error\":{\"type\":\"INTEGRATION_ERROR\",\"code\":\"QBD_DUPLICATE_NAME\",\"httpStatusCode\":409,\"requestId\":\"req_1\",\"message\":\"Duplicate.\"}}";
        var stub = new StubHandler().Respond(200, failed);
        using var client = stub.Client();
        var request = await client.Requests.RetrieveAsync("req_1");
        var ex = Assert.Throws<IntegrationException>(() => ApiCore.TryResolve(request, s => s, out _));
        Assert.Equal(409, ex.Status);
        Assert.Equal(ErrorCodes.QbdDuplicateName, ex.Code);
        Assert.Equal("Duplicate.", ex.Message);
    }

    [Fact]
    public async Task With_response_exposes_status_headers_and_request_id()
    {
        var stub = new StubHandler().Respond(200, "{\"id\":\"1\"}", ("Daapi-Request-Id", "req_raw"), ("Daapi-Warnings", "1"));
        using var client = stub.Client();
        var response = await client.Qbd.Customers.RetrieveWithResponseAsync("1");
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("req_raw", response.RequestId);
        Assert.Equal("1", response.Warnings);
        Assert.Equal("1", response.Data.Id);
    }

    [Fact]
    public async Task Path_parameters_are_escaped_and_required()
    {
        var stub = new StubHandler().Respond(200, "{\"id\":\"a/b c\"}");
        using var client = stub.Client();
        await client.Qbd.Customers.RetrieveAsync("a/b c");
        Assert.EndsWith("/customers/a%2Fb%20c", stub.Requests[0].Request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        await Assert.ThrowsAsync<ArgumentException>(() => client.Qbd.Customers.RetrieveAsync(""));
    }

    [Fact]
    public void Missing_key_names_the_environment_variable()
    {
        if (Environment.GetEnvironmentVariable("DAAPI_SECRET_KEY") is not null) return;
        var ex = Assert.Throws<DaapiException>(() => new DesktopAccountingApiClient(new ClientOptions()));
        Assert.Contains("DAAPI_SECRET_KEY", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_key_checksum()
    {
        Assert.True(ApiKeys.IsValid(StubHandler.Key));
        Assert.False(ApiKeys.IsValid(StubHandler.Key.Substring(0, StubHandler.Key.Length - 1) + "A"));
        Assert.Equal("0nQFLR", ApiKeys.Checksum("Conformance0Key0For0SDK0Tests00001"));
    }
}
