# Desktop Accounting API .NET SDK

The C# and .NET client for [Desktop Accounting API](https://www.desktopaccountingapi.com/), a REST API for QuickBooks Desktop and QuickBooks Enterprise. Your server makes typed calls such as `client.Qbd.Invoices.CreateAsync(...)`, and Desktop Accounting API delivers them to your customer's company file through the QuickBooks Web Connector.

- Covers all 275 operations of API 1.0.0: QuickBooks objects and reports (`client.Qbd`), end users, auth sessions, request tracking, qbXML passthrough and webhook verification.
- Models for every object, nullable annotations, `async` methods with `CancellationToken`, `IAsyncEnumerable` pagination and typed exceptions for every error type.
- Amounts are `decimal`, sent with their scale preserved.
- Every write carries an idempotency key, retries happen only where they cannot duplicate data, and an expired QuickBooks cursor never restarts a list silently.
- `netstandard2.0` (.NET Framework 4.6.2+) and `net8.0` builds.

[Documentation](https://www.desktopaccountingapi.com/docs/) · [API reference](https://www.desktopaccountingapi.com/docs/api/reference/) · [Every SDK method](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/blob/main/api.md) · [Examples](https://github.com/DesktopAccountingAPI/examples/tree/main/dotnet) · [Changelog](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/blob/main/CHANGELOG.md) · [Status](https://status.desktopaccountingapi.com)

## Install

The package is [`DesktopAccountingAPI.QuickBooksDesktop`](https://www.nuget.org/packages/DesktopAccountingAPI.QuickBooksDesktop) on NuGet. The current version is **0.1.1**:

```sh
dotnet add package DesktopAccountingAPI.QuickBooksDesktop --version 0.1.1
```

Or in your project file:

```xml skip
<PackageReference Include="DesktopAccountingAPI.QuickBooksDesktop" Version="0.1.1" />
```

The namespace is `DesktopAccountingApi.QuickBooksDesktop`; models are in `DesktopAccountingApi.QuickBooksDesktop.Models`.

## Requirements

- .NET 8 or later, or any runtime that supports .NET Standard 2.0 (including .NET Framework 4.6.2 and later).
- A server-side application. Secret keys must never reach a browser, a Blazor WebAssembly app or a desktop app you distribute (see [Authentication](#authentication)).

## Authentication

1. Sign in to the [dashboard](https://www.desktopaccountingapi.com/dashboard) and open **API keys**.
2. Create a secret key. Test projects issue `sk_test_...` keys; production projects issue `sk_live_...` keys. Choose **Read-only** for reporting jobs and AI agents that must never change data. The full key is shown once.
3. Put it in the `DAAPI_SECRET_KEY` environment variable of your server (or your secret store):

```sh
export DAAPI_SECRET_KEY="sk_test_..."
```

`new DesktopAccountingApiClient()` reads `DAAPI_SECRET_KEY` (and `DAAPI_BASE_URL`, if set). You can also set `ClientOptions.ApiKey`, for example from `IConfiguration`. The SDK checks the key's format and checksum locally, so a mistyped key fails before any network call.

A secret key can read and write every connected company file in its project. Keep it on your server, in a secret manager or environment variable. Never ship it to a browser or client app, and never commit it. The API refuses browser requests from other origins on purpose. If a key leaks, revoke it in the dashboard and create a new one. See [Authentication and API keys](https://www.desktopaccountingapi.com/docs/get-started/authentication/).

## Quickstart

Each of your customers is an **end user** (`eu_...`) with one QuickBooks Desktop company file, connected through the Web Connector. Copy an end user ID from the dashboard's **End users** page, then in a console app (`dotnet new console`):

```csharp run=quickstart harness=none
using System;
using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;

// Reads DAAPI_SECRET_KEY. EndUserId is sent as Daapi-End-User-Id on every QuickBooks call.
using var client = new DesktopAccountingApiClient(new ClientOptions { EndUserId = "eu_01j9x4m6v4c8k2t7q0r5s3w1zb" });

var health = await client.Qbd.HealthCheckAsync();
Console.WriteLine($"QuickBooks connection: {health.Status}");

await foreach (var invoice in client.Qbd.Invoices.ListAsync(new InvoiceListParams { Limit = 10 }))
{
    Console.WriteLine($"{invoice.RefNumber} {invoice.Subtotal}"); // Subtotal is a decimal, for example 105.50
}
```

The client is thread-safe; create one and reuse it (for example as a singleton in dependency injection). `Dispose()` releases the `HttpClient` the SDK created.

## End users

QuickBooks Desktop operations (`client.Qbd...`) act on one end user's company file and send the `Daapi-End-User-Id` header. Set a default in `ClientOptions`, derive a client per end user, or pass it per call:

```csharp
var acme = client.ForEndUser("eu_01j9x4m6v4c8k2t7q0r5s3w1zb"); // shares the connection pool
var customers = await acme.Qbd.Customers.ListAsync().GetFirstPageAsync();

await client.Qbd.HealthCheckAsync(new RequestOptions { EndUserId = "eu_01j9x4m6v4c8k2t7q0r5s3w1zb" }); // or per call
```

A QuickBooks call without an end user throws `DaapiException` before anything is sent. Platform operations (`client.EndUsers`, `client.AuthSessions`, `client.Requests`) never send the header. Create end users and their setup links with `client.EndUsers.CreateAsync` and `client.AuthSessions.CreateAsync`; see [End users](https://www.desktopaccountingapi.com/docs/connect/end-users/).

## Common workflows

### List records with auto-pagination

`await foreach` walks every page. The SDK requests the next page while you process the current one, so slow loop bodies stay inside the QuickBooks cursor's idle window.

```csharp
await foreach (var customer in client.Qbd.Customers.ListAsync(new CustomerListParams { Limit = 100, UpdatedAfter = "2026-01-01" }))
{
    Console.WriteLine($"{customer.Id} {customer.FullName} {customer.Balance}");
}

var page = await client.Qbd.Customers.ListAsync(new CustomerListParams { Limit = 100 }).GetFirstPageAsync(); // only the first page
Console.WriteLine($"{page.Data.Count} {page.HasMore} {page.NextCursor}");
```

More options, and what to do when a cursor expires, are in [Pagination](#pagination).

### Create a record with an idempotency key

Every write sends an `Idempotency-Key`. Pass your own, derived from your data, so a retry after a crash or timeout returns the first result instead of creating a duplicate:

```csharp
var invoice = await client.Qbd.Invoices.CreateAsync(
    new InvoiceCreateInput
    {
        CustomerId = "80000001-1700000000",
        TransactionDate = new DateOnly(2026, 10, 5),
        RefNumber = "WEB-8812",
        Lines = new[]
        {
            new InvoiceLineCreateInput { ItemId = "80000005-1700000000", Quantity = 2, Rate = 52.75m },
        },
    },
    new RequestOptions { IdempotencyKey = "order-8812-invoice" });
Console.WriteLine($"{invoice.Id} {invoice.RefNumber} {invoice.Subtotal}"); // Subtotal 105.50
```

### Update a record with its revision number

QuickBooks rejects an update unless it carries the object's current `RevisionNumber`, so concurrent edits are never overwritten. Read the object, then send its `RevisionNumber` with only the properties you change:

```csharp
var current = await client.Qbd.Invoices.RetrieveAsync("7-1700000000");
try
{
    var updated = await client.Qbd.Invoices.UpdateAsync(current.Id, new InvoiceUpdateInput
    {
        RevisionNumber = current.RevisionNumber,
        Memo = "Paid by card",
    });
    Console.WriteLine(updated.RevisionNumber); // the new revision
}
catch (IntegrationException ex) when (ex.Code == ErrorCodes.QbdRevisionNumberStale)
{
    // Someone changed the invoice after you read it. Retrieve it again, reapply your change,
    // and update with the new RevisionNumber.
}
```

A stale revision is a `409` `INTEGRATION_ERROR` with code `QBD_REVISION_NUMBER_STALE`. Nothing was changed (`outcome: "not_applied"`):

```json
{
  "error": {
    "type": "INTEGRATION_ERROR",
    "code": "QBD_REVISION_NUMBER_STALE",
    "message": "The object changed since you read it; revisionNumber is out of date.",
    "userFacingMessage": "This record was changed by someone else. Reload it and try again.",
    "httpStatusCode": 409,
    "integrationCode": "3200",
    "requestId": "req_01j9x4m6v4c8k2t7q0r5s3w1zd",
    "cause": "QuickBooks rejects updates that do not carry the current revision number, so concurrent edits are not lost.",
    "fixes": [{ "actor": "developer", "action": "Retrieve the object, merge your change, and update with the new revisionNumber." }],
    "docsUrl": "https://www.desktopaccountingapi.com/docs/errors/#qbd_revision_number_stale",
    "retryable": false,
    "outcome": "not_applied",
    "param": null,
    "details": {}
  }
}
```

### Handle errors

Exceptions are typed by the API's error `type`, and every API error carries the request ID, a message you can show your end user, the cause, concrete fixes and a link to its documentation:

```csharp
try
{
    await client.Qbd.Customers.RetrieveAsync("80000099-1700000000");
}
catch (IntegrationConnectionException ex)
{
    // QuickBooks is closed, a dialog is open, or the Web Connector is not running: the end user has to act.
    ShowToEndUser(ex.UserFacingMessage ?? ex.Message);
}
catch (ApiException ex)
{
    Console.Error.WriteLine($"{ex.Status} {ex.Code}: {ex.Message} (request {ex.RequestId})");
    Console.Error.WriteLine($"{ex.Cause} {ex.DocsUrl}");
    foreach (var fix in ex.Fixes) Console.Error.WriteLine($"{fix.Actor}: {fix.Action}");
}
```

Every class and property is listed in [Errors](#errors). The [error catalog](https://www.desktopaccountingapi.com/docs/errors/) documents every code.

### Run a request asynchronously and get a webhook

QuickBooks only processes requests while the end user's Web Connector is running. Each resource's `Enqueue` accessor queues a request and returns at once with a handle; the API also sends a `request.succeeded` or `request.failed` webhook when it finishes:

```csharp
var handle = await client.Qbd.Invoices.Enqueue.CreateAsync(
    new InvoiceCreateInput { CustomerId = "80000001-1700000000" },
    new RequestOptions { QueueTtl = TimeSpan.FromHours(1), IdempotencyKey = "order-8813-invoice" });
Console.WriteLine($"{handle.Id} is {handle.Request.Status}"); // req_... is queued
Invoice invoice = await handle.WaitAsync(TimeSpan.FromMinutes(2)); // the typed Invoice, or the typed exception
Console.WriteLine(invoice.RefNumber);
```

Verify each webhook delivery with the endpoint's signing secret before you trust it. Pass the raw body, not parsed JSON. With ASP.NET Core minimal APIs:

```csharp harness=webhook
using System.IO;
using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Microsoft.AspNetCore.Http;

app.MapPost("/webhooks", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    try
    {
        var ev = WebhookVerifier.Verify(body, name => request.Headers[name].ToString(), secret);
        if (ev.Type == WebhookEventTypes.RequestSucceeded) Console.WriteLine($"request {ev.DataAs<Request>()?.Id} succeeded");
        return Results.NoContent();
    }
    catch (WebhookVerificationException)
    {
        return Results.BadRequest();
    }
});
```

Create webhook endpoints and copy their `whsec_...` signing secrets in the dashboard under **Webhooks**. Details: [Async requests](#async-requests), [Webhooks](#webhooks), and the [webhooks guide](https://www.desktopaccountingapi.com/docs/guides/webhooks/).

### Set timeouts and retries

```csharp
using var patient = new DesktopAccountingApiClient(new ClientOptions
{
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 4,
    ServerTimeout = TimeSpan.FromSeconds(25),
});
await patient.Qbd.Invoices.RetrieveAsync("7-1700000000", new RequestOptions { EndUserId = "eu_01j9x4m6v4c8k2t7q0r5s3w1zb", MaxRetries = 0 });
```

`Timeout` is the client's limit per HTTP attempt; `ServerTimeout` is how long the API waits for QuickBooks. Reads and writes retry only when it is safe; see [Retries and idempotency](#retries-and-idempotency) and [Timeouts](#timeouts).

## Configuration

| `ClientOptions` property | Default | Meaning |
| --- | --- | --- |
| `ApiKey` | `DAAPI_SECRET_KEY` | Secret key, `sk_live_...` or `sk_test_...`. Checked locally (format and checksum) when the client is created; a malformed key throws `DaapiException` before any request. |
| `BaseUrl` | `DAAPI_BASE_URL`, else `https://api.desktopaccountingapi.com` | API base URL. May include a path prefix. |
| `EndUserId` | none | Default end user (`eu_...`) for QuickBooks Desktop operations. |
| `Timeout` | 100 s | Client-side timeout per HTTP attempt, and the total wait for a request that timed out on the server. |
| `MaxRetries` | 2 | Retries after network errors, `429` and retryable `5xx`. `0` disables retries. |
| `ServerTimeout` | server default | Sent as `Daapi-Timeout-Seconds` on operations that accept it. |
| `HttpClient` | SDK-owned | Your own `HttpClient`. The SDK does not dispose it or change its settings. |
| `HttpMessageHandler` | SDK-owned | A handler for the SDK's `HttpClient` (proxy, instrumentation, test double). |
| `Logger` | none | `Action<DaapiLogLevel, string>`: one line per HTTP attempt, retry and long poll. The SDK never logs the API key, the `Authorization` header or request and response bodies. |

Every method takes an optional `RequestOptions` (`EndUserId`, `IdempotencyKey`, `Timeout`, `MaxRetries`, `ServerTimeout`, `QueueTtl`) and a `CancellationToken`.

## Inputs: omitted, set and null

Request bodies and query parameters are classes such as `InvoiceUpdateInput` and `InvoiceListParams`. Only the properties you set are sent. Properties documented as "Set to null to clear" send an explicit `null`, which clears the value in QuickBooks; setting any other property to `null` removes it from the request. `IsSet(name)` and `Unset(name)` inspect and undo assignments. Required properties that are missing throw `DaapiException` before the request is sent.

```csharp
var invoice = await client.Qbd.Invoices.RetrieveAsync("7-1700000000");
await client.Qbd.Invoices.UpdateAsync(invoice.Id, new InvoiceUpdateInput
{
    RevisionNumber = invoice.RevisionNumber,
    Memo = null, // sends "memo": null and clears the memo
}); // other fields are not sent and stay unchanged
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
Console.WriteLine($"{page.Data.Count} {page.NextCursor} {page.HasMore} {page.RemainingCount} {page.CursorExpiresAt}");

await foreach (var p in client.Qbd.Customers.ListAsync().PagesAsync()) Console.WriteLine(p.Data.Count); // one page at a time
List<Customer> all = await client.Qbd.Customers.ListAsync().ListAllAsync();
```

Continue requests send only `cursor` (and `limit` if you set one). A network error on a continue request retries the same cursor, which returns the same page.

QuickBooks cursors live inside one QuickBooks session and expire when it ends or after an idle period. Then iteration throws `CursorExpiredException` with `ItemsYielded`, `PagesServed`, `LastId`, `LastUpdatedAt` (as the API sent it) and `Reason`. The SDK never restarts a list on its own, because records may have changed in the meantime. Restart with a watermark and skip what you already have:

```csharp
var seen = new HashSet<string>();
try
{
    await foreach (var customer in client.Qbd.Customers.ListAsync(new CustomerListParams { Limit = 100 })) seen.Add(customer.Id);
}
catch (CursorExpiredException ex)
{
    Console.WriteLine($"{ex.ItemsYielded} items, {ex.PagesServed} pages, last {ex.LastId}: {ex.Reason}");
    var resume = new CustomerListParams { Limit = 100 };
    if (ex.LastUpdatedAt is not null) resume.UpdatedAfter = ex.LastUpdatedAt;
    await foreach (var customer in client.Qbd.Customers.ListAsync(resume)) seen.Add(customer.Id);
}
```

Lists without pagination (accounts, classes, terms and other small lists) return their list envelope directly. The [pagination guide](https://www.desktopaccountingapi.com/docs/guides/pagination/) explains cursor lifetimes.

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
| `RequestPendingException` | A request is still running when the SDK stops waiting (see [Timeouts](#timeouts)). |
| `WebhookVerificationException` | A webhook failed verification. |

`ApiException` exposes every field of the API error: `Status` (HTTP status), `Type`, `Code`, `Message`, `UserFacingMessage`, `HttpStatusCode`, `IntegrationCode`, `RequestId` (from the error, else the `Daapi-Request-Id` header), `Cause` (why the error happens; this is the API's `cause` field, not the .NET exception chain, which stays in `InnerException`), `Fixes` (`Actor`, `Action`), `DocsUrl`, `Retryable`, `Outcome`, `Param`, `Details` and the response `Headers`. `ErrorCodes` and `ErrorTypes` hold a constant for every code and type in the contract:

```csharp
try
{
    await client.Qbd.Customers.RetrieveAsync("80000099-1700000000");
}
catch (IntegrationConnectionException ex) when (ex.Code == ErrorCodes.QbdModalDialogOpen)
{
    ShowToEndUser(ex.UserFacingMessage ?? ex.Message);
}
catch (IntegrationException ex) when (ex.Code == ErrorCodes.QbdObjectNotFound)
{
    Console.WriteLine(ex.UserFacingMessage);
    foreach (var fix in ex.Fixes) Console.WriteLine($"{fix.Actor}: {fix.Action}");
    Console.WriteLine($"{ex.DocsUrl} (request {ex.RequestId})");
}
```

Include the `RequestId` when you contact support. See the [error handling guide](https://www.desktopaccountingapi.com/docs/guides/error-handling/).

## Retries and idempotency

The SDK retries network errors that happen before a response arrives (connection failures, resets, dropped connections, client timeouts), `429` responses, and `5xx` responses that carry `Daapi-Should-Retry: true`, up to `MaxRetries` (default 2). It never retries when the response says `Daapi-Should-Retry: false`, when the error `outcome` is `unknown` or `pending`, or when an error response is not JSON. Backoff starts at 0.5 s and doubles up to 8 s with jitter; a `Retry-After` header (seconds, including `0`, or an HTTP date) takes precedence.

Every write (create, update, delete, void, passthrough) sends an `Idempotency-Key`. The SDK generates a UUID per call and reuses it on every retry of that call, so a retried create cannot create a second record. Pass your own key (`RequestOptions.IdempotencyKey`) to make a create safe across process restarts. See the [idempotency guide](https://www.desktopaccountingapi.com/docs/guides/idempotency/).

## Timeouts

There are two timeouts:

- **Client timeout** (`ClientOptions.Timeout` / `RequestOptions.Timeout`, default 100 s): how long the SDK waits for each HTTP attempt.
- **Server timeout** (`ServerTimeout`, sent as `Daapi-Timeout-Seconds`): how long the API waits for QuickBooks before answering `504 QBD_REQUEST_TIMEOUT`.

After `504 QBD_REQUEST_TIMEOUT` the request is still queued or running at the end user's QuickBooks. The SDK does not resend it. It long-polls `GET /v1/requests/{id}?waitSeconds=N` until the call's client timeout has passed since the call started. When the request succeeds you get the normal typed result; when it fails you get the typed exception; when time runs out you get `RequestPendingException` with `RequestId`:

```csharp
try
{
    await client.Qbd.Invoices.CreateAsync(
        new InvoiceCreateInput { CustomerId = "80000001-1700000000" },
        new RequestOptions { IdempotencyKey = "order-8814-invoice" });
}
catch (RequestPendingException ex)
{
    var request = await client.Requests.RetrieveAsync(ex.RequestId);
    Console.WriteLine(request.Status); // still "queued" or "running"; a webhook reports the result
}
```

## Async requests

Operations that QuickBooks runs can also be queued without waiting. Each resource with such operations has an `Enqueue` accessor with the same methods, returning a `RequestHandle<T>`:

```csharp
var input = new InvoiceCreateInput { CustomerId = "80000001-1700000000" };
var handle = await client.Qbd.Invoices.Enqueue.CreateAsync(input, new RequestOptions { QueueTtl = TimeSpan.FromHours(1) });
Console.WriteLine($"{handle.Id} is {handle.Request.Status}");

Invoice invoice = await handle.WaitAsync(TimeSpan.FromMinutes(5)); // long-polls; typed result or typed exception
Request current = await handle.StatusAsync(); // one GET, no waiting
Invoice result = await handle.ResultAsync(); // throws RequestPendingException if not finished
```

The request is sent with `Prefer: respond-async` (and `Daapi-Queue-Ttl-Seconds` when `QueueTtl` is set). A `request.succeeded` or `request.failed` webhook tells you when it finished. See the [request lifecycle guide](https://www.desktopaccountingapi.com/docs/guides/request-lifecycle/).

## Webhooks

Webhooks follow [Standard Webhooks](https://www.standardwebhooks.com/). `WebhookVerifier.Verify(body, headers, secret)` checks the signature and timestamp and returns the parsed event; no API key is needed. `client.Webhooks.Verify(...)` does the same. Header names are matched case-insensitively, the secret may include the `whsec_` prefix or not, several `v1,` signatures are accepted during secret rotation, signatures are compared in constant time, and timestamps more than 5 minutes from the clock are rejected (`WebhookVerifyOptions.Tolerance`, `WebhookVerifyOptions.Clock`). `WebhookVerifier.VerifySignature` checks only the signature; `WebhookVerifier.Sign` creates one for tests. Delivery is at least once: deduplicate on `ev.Id`.

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
var json = await client.EndUsers.PassthroughAsync("eu_01j9x4m6v4c8k2t7q0r5s3w1zb", new PassthroughInput
{
    ["CustomerQueryRq"] = new Dictionary<string, object?> { ["MaxReturned"] = 5 },
});

string xml = await client.EndUsers.PassthroughXmlAsync("eu_01j9x4m6v4c8k2t7q0r5s3w1zb", "<CustomerQueryRq><MaxReturned>5</MaxReturned></CustomerQueryRq>");
```

Passthrough calls always send an idempotency key, because a body with anything other than a query element is a write.

## Versioning and changelog

- The package follows [semantic versioning](https://semver.org/). Only a major version removes or renames anything in the SDK's public API.
- The .NET, Node.js, Python and Java SDKs and the [MCP server](https://github.com/DesktopAccountingAPI/quickbooks-desktop-mcp) are released together with the same version number, generated from the same API contract.
- Every release is listed in [CHANGELOG.md](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/blob/main/CHANGELOG.md) and tagged `v<version>` on GitHub.
- The API is versioned in its path (`/v1`). Within `v1` the API only adds operations, fields, enum values and error codes. Unknown fields are kept and unknown enum values pass through, so older SDK versions keep working.
- `.daapi-sdk.json` records the API contract digest (sha256 `1cc3058cecb5...` for this release), the generator version and the list of generated files; `DesktopAccountingApiClient.ContractSha256` exposes the same digest at runtime.

## Support

- [Documentation](https://www.desktopaccountingapi.com/docs/), the [API reference](https://www.desktopaccountingapi.com/docs/api/reference/) and the [error catalog](https://www.desktopaccountingapi.com/docs/errors/).
- [Status page](https://status.desktopaccountingapi.com) for API and connection incidents.
- SDK bugs and feature requests: [GitHub issues](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/issues).
- Questions about your account, keys, billing or a specific end user's connection: [contact us](https://www.desktopaccountingapi.com/contact). Include the `RequestId` of a failing call, never your secret key.
- Security reports: use **Report a vulnerability** on the repository's Security tab.

## Development

```sh
mise install
mise run check
```

`mise run check` restores, builds both target frameworks with warnings as errors and analyzers on, verifies formatting, runs the unit tests and the cross-language conformance suite (it starts `node conformance/mock-server.mjs`), builds `examples/`, compiles every C# sample in this README and runs the quickstart against the mock server, packs the NuGet package and inspects its contents. Code under `src/DesktopAccountingApi.QuickBooksDesktop/Generated`, `api.md`, `conformance/fixtures` and this README are generated from the API contract; see [CONTRIBUTING.md](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/blob/main/CONTRIBUTING.md).

To build from source, clone the repository and reference `src/DesktopAccountingApi.QuickBooksDesktop/DesktopAccountingApi.QuickBooksDesktop.csproj`. Releases are published by `.github/workflows/publish.yml` when a `v*` tag is pushed.

## License

MIT. See [LICENSE](https://github.com/DesktopAccountingAPI/quickbooks-desktop-dotnet/blob/main/LICENSE).

QuickBooks is a registered trademark of Intuit Inc. Desktop Accounting API is an independent product and is not affiliated with, endorsed by, or approved by Intuit Inc.
