# Desktop Accounting API .NET SDK

The official .NET client for [Desktop Accounting API](https://www.desktopaccountingapi.com/docs/), a REST API for QuickBooks Desktop. It covers all 275 API operations with typed models and async methods, and handles pagination, retries with idempotency keys, timed-out requests, async requests and webhook verification.

- Package: `DesktopAccountingAPI.QuickBooksDesktop` 0.1.0
- Namespace: `DesktopAccountingApi.QuickBooksDesktop` (models in `.Models`)
- Targets: `netstandard2.0` (.NET Framework 4.6.2+, .NET Core 2.0+) and `net8.0`
- Generated from API contract 1.0.0 (sha256 `1cc3058cecb5`); [api.md](api.md) lists every method

## Requirements

.NET 8 or later, or any runtime that supports .NET Standard 2.0 (including .NET Framework 4.6.2 and later). Building this repository needs the .NET 8 SDK and Node.js (for the conformance mock server); `mise install` provides both at the pinned versions.

## Install

```sh
dotnet add package DesktopAccountingAPI.QuickBooksDesktop
```

### Install from source

```sh
git clone https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet.git
dotnet add reference quickbooks-desktop-dotnet/src/DesktopAccountingApi.QuickBooksDesktop/DesktopAccountingApi.QuickBooksDesktop.csproj
```

## Quickstart

```csharp
using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;

// Reads DAAPI_SECRET_KEY from the environment.
using var client = new DesktopAccountingApiClient(new ClientOptions { EndUserId = "eu_..." });

var health = await client.Qbd.HealthCheckAsync();
Console.WriteLine($"Connected to {health.Quickbooks.CompanyName}");

var invoice = await client.Qbd.Invoices.CreateAsync(new InvoiceCreateInput
{
    CustomerId = "80000001-1700000000",
    TransactionDate = new DateOnly(2026, 10, 5),
    Lines = new[]
    {
        new InvoiceLineCreateInput { ItemId = "80000005-1700000000", Quantity = 2, Rate = 52.75m },
    },
});
Console.WriteLine($"{invoice.RefNumber}: {invoice.Subtotal}");
```

[examples/Quickstart](examples/Quickstart) is a runnable version. More examples (idempotent creates, network failures, pagination with cursor expiry, async requests with a webhook receiver, error handling) are in the [examples repository](https://github.com/DesktopAccountingAPI/examples/tree/main/dotnet).

## Configuration

| `ClientOptions` property | Default | Meaning |
| --- | --- | --- |
| `ApiKey` | `DAAPI_SECRET_KEY` | Secret key, `sk_live_...` or `sk_test_...`. Checked locally (format and checksum) when the client is created; a malformed key throws `DaapiException` before any request. |
| `BaseUrl` | `DAAPI_BASE_URL`, else `https://api.desktopaccountingapi.com` | API base URL. May include a path prefix. Staging: `https://api-staging.desktopaccountingapi.com`. |
| `EndUserId` | none | Default end user (`eu_...`) for QuickBooks Desktop operations. |
| `Timeout` | 100 s | Client-side timeout per HTTP attempt, and the total wait for a request that timed out on the server. |
| `MaxRetries` | 2 | Retries after network errors, `429` and retryable `5xx`. `0` disables retries. |
| `ServerTimeout` | server default | Sent as `Daapi-Timeout-Seconds` on operations that accept it. |
| `HttpClient` | SDK-owned | Your own `HttpClient`. The SDK does not dispose it or change its settings. |
| `HttpMessageHandler` | SDK-owned | A handler for the SDK's `HttpClient` (proxy, instrumentation, test double). |
| `Logger` | none | `Action<DaapiLogLevel, string>`: one line per HTTP attempt, retry and long poll. The SDK never logs the API key, the `Authorization` header or request and response bodies. |

Every method takes an optional `RequestOptions` (`EndUserId`, `IdempotencyKey`, `Timeout`, `MaxRetries`, `ServerTimeout`, `QueueTtl`) and a `CancellationToken`. The client is thread-safe; create one and reuse it. `Dispose()` releases the `HttpClient` the SDK created.

## End users

QuickBooks Desktop operations (`client.Qbd...`) act on one end user's company file and need an end user ID. Platform operations (`client.EndUsers`, `client.AuthSessions`, `client.Requests`) do not, and the SDK never sends `Daapi-End-User-Id` on them. A QuickBooks operation without an end user throws `DaapiException` before sending anything.

```csharp
var acme = client.ForEndUser("eu_01j9x4m6v4c8k2t7q0r5s3w1zb"); // shares the connection pool
var customers = await acme.Qbd.Customers.ListAsync().GetFirstPageAsync();

// or per call
await client.Qbd.HealthCheckAsync(new RequestOptions { EndUserId = "eu_..." });
```

## Inputs: omitted, set and null

Request bodies and query parameters are classes such as `InvoiceUpdateInput` and `InvoiceListParams`. Only the properties you set are sent. Properties documented as "Set to null to clear" send an explicit `null`, which clears the value in QuickBooks; setting any other property to `null` removes it from the request. `IsSet(name)` and `Unset(name)` inspect and undo assignments. Required properties that are missing throw `DaapiException` before the request is sent.

```csharp
await client.Qbd.Invoices.UpdateAsync("7-1700000000", new InvoiceUpdateInput
{
    RevisionNumber = "1700000007",
    Memo = null,               // sends "memo": null and clears the memo
});                            // other fields are not sent and stay unchanged
```

Types follow the contract: money and other decimals are `decimal` and travel as strings with their scale preserved (`5.00m` is sent as `"5.00"`); dates are `DateOnly` (`YYYY-MM-DD`; on .NET Standard 2.0 through the `Portable.System.DateTimeOnly` package); timestamps are `DateTimeOffset` with the offset QuickBooks reported. Enums whose values can grow are `string` properties with a constants class listing the known values (`RequestStatus.Succeeded`); unknown values pass through. Enums the API fixes for inputs are C# enums (`PaymentStatus.NotPaid`). Response models ignore missing fields and keep unknown fields in `AdditionalProperties`. `model.ToJson()` and `DesktopAccountingApiJson.Options` serialize any model back to the wire format.

## Pagination

Cursor-paginated lists return a `Pager<T>`:

```csharp
// Every invoice, page after page.
await foreach (var invoice in client.Qbd.Invoices.ListAsync(new InvoiceListParams { Limit = 100 }))
{
    Console.WriteLine(invoice.RefNumber);
}

var page = await client.Qbd.Invoices.ListAsync(new InvoiceListParams { Limit = 10 }).GetFirstPageAsync();
// page.Data, page.NextCursor, page.HasMore, page.RemainingCount, page.CursorExpiresAt

await foreach (var p in client.Qbd.Customers.ListAsync().PagesAsync()) { /* one page at a time */ }
List<Customer> all = await client.Qbd.Customers.ListAsync().ListAllAsync();
```

The pager requests page N+1 as soon as page N arrives, so the next request stays inside the cursor's idle window while you process the current page. Continue requests send only `cursor` (and `limit` if you set one). A network error on a continue request retries the same cursor, which returns the same page.

QuickBooks cursors live inside one QuickBooks session and expire when it ends or after an idle period. Then iteration throws `CursorExpiredException` with `ItemsYielded`, `PagesServed`, `LastId`, `LastUpdatedAt` (as the API sent it) and `Reason`. The SDK never restarts a list on its own, because records may have changed in the meantime. Restart with a watermark and skip what you already have:

```csharp
catch (CursorExpiredException ex)
{
    var resume = client.Qbd.Customers.ListAsync(new CustomerListParams { UpdatedAfter = ex.LastUpdatedAt, Limit = 100 });
    // skip IDs you already processed
}
```

Lists without pagination (`x-daapi-pagination: none`, such as accounts and classes) return their list envelope directly.

## Errors

Every exception derives from `DaapiException`:

| Class | When |
| --- | --- |
| `DaapiException` | Client-side problems: missing or malformed API key, missing end user, missing required input, unparseable response. |
| `ApiException` | Any error response from the API (base class; an unknown error `type` stays this class). |
| `InvalidRequestException` (`CursorExpiredException`) | `INVALID_REQUEST_ERROR` |
| `AuthenticationException` | `AUTHENTICATION_ERROR` |
| `PermissionException` | `PERMISSION_ERROR` |
| `BillingException` | `BILLING_ERROR` |
| `RateLimitException` | `RATE_LIMIT_ERROR` |
| `IntegrationConnectionException` | `INTEGRATION_CONNECTION_ERROR` (Web Connector not running, company file closed, modal dialog open) |
| `IntegrationException` | `INTEGRATION_ERROR` (QuickBooks rejected the request) |
| `OutcomeUnknownException` | `OUTCOME_UNKNOWN_ERROR` (a write was sent and its result is unknown) |
| `InternalException` | `INTERNAL_ERROR` |
| `ApiConnectionException` (`ApiTimeoutException`) | No response after all retries. |
| `RequestPendingException` | A request is still running when the SDK stops waiting (see Timeouts). |
| `WebhookVerificationException` | A webhook failed verification. |

`ApiException` exposes every field of the API error: `Status` (HTTP status), `Type`, `Code`, `Message`, `UserFacingMessage`, `HttpStatusCode`, `IntegrationCode`, `RequestId` (from the error, else the `Daapi-Request-Id` header), `Cause` (why the error happens; this is the API's `cause` field, not the .NET exception chain, which stays in `InnerException`), `Fixes` (`Actor`, `Action`), `DocsUrl`, `Retryable`, `Outcome`, `Param`, `Details` and the response `Headers`. `ErrorCodes` and `ErrorTypes` hold a constant for every code and type in the contract.

```csharp
try
{
    await client.Qbd.Customers.RetrieveAsync("80000099-1700000000");
}
catch (IntegrationException ex) when (ex.Code == ErrorCodes.QbdObjectNotFound)
{
    Console.WriteLine(ex.UserFacingMessage);
    foreach (var fix in ex.Fixes) Console.WriteLine($"{fix.Actor}: {fix.Action}");
    Console.WriteLine($"{ex.DocsUrl} (request {ex.RequestId})");
}
```

## Retries and idempotency

The SDK retries network errors that happen before a response arrives (connection failures, resets, dropped connections, client timeouts), `429` responses, and `5xx` responses that carry `Daapi-Should-Retry: true`. It never retries when the response says `Daapi-Should-Retry: false`, when the error `outcome` is `unknown` or `pending`, or when an error response is not JSON. Backoff starts at 0.5 s and doubles up to 8 s with jitter; a `Retry-After` header (seconds, including `0`, or an HTTP date) takes precedence.

Every write (create, update, delete, void, passthrough) sends an `Idempotency-Key`. The SDK generates a UUID per call and reuses it on every retry of that call, so a retried create cannot create a second record. Pass your own key to make a create safe across process restarts:

```csharp
await client.Qbd.Invoices.CreateAsync(input, new RequestOptions { IdempotencyKey = $"order-{orderId}-invoice" });
```

## Timeouts

There are two timeouts:

- **Client timeout** (`ClientOptions.Timeout` / `RequestOptions.Timeout`, default 100 s): how long the SDK waits for each HTTP attempt.
- **Server timeout** (`ServerTimeout`, sent as `Daapi-Timeout-Seconds`): how long the API waits for QuickBooks before answering `504 QBD_REQUEST_TIMEOUT`.

After `504 QBD_REQUEST_TIMEOUT` the request is still queued or running at the end user's QuickBooks. The SDK does not resend it. It long-polls `GET /v1/requests/{id}?waitSeconds=N` until the call's client timeout has passed since the call started. When the request succeeds you get the normal typed result; when it fails you get the typed exception; when time runs out you get `RequestPendingException` with `RequestId`, which you can check later with `client.Requests.RetrieveAsync(id)` or a webhook.

## Async requests

Operations that QuickBooks runs can also be queued without waiting. Each resource with such operations has an `Enqueue` accessor with the same methods, returning a `RequestHandle<T>`:

```csharp
var handle = await client.Qbd.Invoices.Enqueue.CreateAsync(input, new RequestOptions { QueueTtl = TimeSpan.FromHours(1) });
Console.WriteLine($"{handle.Id} is {handle.Request.Status}");

Invoice invoice = await handle.WaitAsync(TimeSpan.FromMinutes(5)); // long-polls; typed result or typed exception
Request current = await handle.StatusAsync();                       // one GET, no waiting
Invoice result = await handle.ResultAsync();                        // throws RequestPendingException if not finished
```

The request is sent with `Prefer: respond-async` (and `Daapi-Queue-Ttl-Seconds` when `QueueTtl` is set). A `request.succeeded` or `request.failed` webhook tells you when it finished.

## Webhooks

Webhooks follow [Standard Webhooks](https://www.standardwebhooks.com/). Verify the raw body with the endpoint's signing secret; no API key is needed:

```csharp
// ASP.NET Core minimal API
app.MapPost("/webhooks", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    try
    {
        var ev = WebhookVerifier.Verify(body, name => request.Headers[name].ToString(), secret);
        if (ev.Type == WebhookEventTypes.RequestSucceeded) { /* ev.Data, ev.DataAs<Request>() */ }
        return Results.Ok();
    }
    catch (WebhookVerificationException)
    {
        return Results.BadRequest();
    }
});
```

`client.Webhooks.Verify(...)` does the same. Header names are matched case-insensitively, the secret may include the `whsec_` prefix or not, several `v1,` signatures are accepted during secret rotation, signatures are compared in constant time, and timestamps more than 5 minutes from the clock are rejected (`WebhookVerifyOptions.Tolerance`, `WebhookVerifyOptions.Clock`). `WebhookVerifier.VerifySignature` checks only the signature; `WebhookVerifier.Sign` creates one for tests. Delivery is at least once: deduplicate on `ev.Id`.

## Raw responses

Every non-paginated method has a `...WithResponseAsync` variant that returns `ApiResponse<T>` with `Data`, `StatusCode`, `Headers`, `RequestId` (`Daapi-Request-Id`) and `Warnings` (`Daapi-Warnings`):

```csharp
var response = await client.Qbd.Customers.RetrieveWithResponseAsync("80000001-1700000000");
Console.WriteLine($"{response.StatusCode} {response.RequestId}: {response.Data.Name}");
```

Pages expose the request ID of their response as `Page<T>.RequestId`.

## Passthrough

`client.EndUsers.PassthroughAsync` sends qbXML request elements directly to an end user's QuickBooks Desktop, as JSON or as XML:

```csharp
var json = await client.EndUsers.PassthroughAsync("eu_...", new PassthroughInput
{
    ["CustomerQueryRq"] = new Dictionary<string, object?> { ["MaxReturned"] = 5 },
});

string xml = await client.EndUsers.PassthroughXmlAsync("eu_...", "<CustomerQueryRq><MaxReturned>5</MaxReturned></CustomerQueryRq>");
```

Passthrough calls always send an idempotency key, because a body with anything other than a query element is a write.

## Versioning

The SDK follows semantic versioning. `.daapi-sdk.json` records the API contract digest, the generator version and the list of generated files for this release; `DesktopAccountingApiClient.ContractSha256` exposes the same digest at runtime. New API fields and enum values do not break older SDK versions: unknown fields are kept and unknown enum values pass through.

## Development

```sh
mise install
mise run check
```

`mise run check` restores, builds both target frameworks with warnings as errors and analyzers on, verifies formatting, runs the unit tests and the cross-language conformance suite (it starts `node conformance/mock-server.mjs`), builds `examples/`, packs the NuGet package and inspects its contents. Code under `src/DesktopAccountingApi.QuickBooksDesktop/Generated`, `api.md` and `conformance/fixtures` is generated from the API contract; see [CONTRIBUTING.md](CONTRIBUTING.md).

Releases are published by `.github/workflows/publish.yml` when a `v*` tag is pushed. `node scripts/publish.mjs release --dry-run` builds, packs, inspects and smoke-tests the package locally without uploading anything.

## Links

- Documentation: https://www.desktopaccountingapi.com/docs/
- Examples: https://github.com/DesktopAccountingAPI/examples
- Changelog: [CHANGELOG.md](CHANGELOG.md)

---

QuickBooks is a registered trademark of Intuit Inc. Desktop Accounting API is an independent product and is not affiliated with, endorsed by, or approved by Intuit Inc.
