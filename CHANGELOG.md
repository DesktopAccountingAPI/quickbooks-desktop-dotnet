# Changelog

## Unreleased

- A read that hits the server timeout (`504 QBD_REQUEST_TIMEOUT` with outcome `not_applicable`) is long-polled like a write: the SDK waits on `GET /v1/requests/{id}` until the call's deadline and returns the result or throws `RequestPendingException`, instead of throwing the 504 at once. The rule is the same in every SDK: HTTP 504, code `QBD_REQUEST_TIMEOUT` and a `details.requestId`, whatever the outcome. Such a 504 is never retried.
- **Breaking:** `ApiResponse<T>.Warnings` is the number of QuickBooks warnings (`int`, 0 when the `Daapi-Warnings` header is absent), not the header text. Read `Headers["Daapi-Warnings"]` for the raw value.
- **Breaking (types):** Response prices, rates and percentages (for example `QbdInvoiceLine.rate`, `QbdSalesOrPurchaseDetail.price`, `ratePercent`) carry the same decimal pattern as their inputs and as amounts, so they are `decimal` instead of `string`.
- The README documents resuming a list from a stored `NextCursor` (`new InvoiceListParams {{ Cursor = savedCursor }}`), now covered by the cross-language conformance suite.
- **Breaking:** `Qbd.Reports.BudgetSummaryAsync` now takes `ReportBudgetSummaryParams` as a required argument, and `FiscalYear` is required in it. The API always rejected a budget report without it (`400 INVALID_PARAMETER`, `param: "fiscalYear"`), so no working call changes behavior; code that omitted it no longer compiles. Set `FiscalYear`, for example `new ReportBudgetSummaryParams { ReportType = ..., FiscalYear = 2026 }`.
- `WebhookEventTypes.ConnectionCompanyFileRemarked` (`connection.company_file_remarked`): the marker that identifies a connection's company file was created, written back after the file lost it (for example a restored backup) or adopted from the file; `data.reason` is `marker_created`, `marker_restored` or `marker_adopted`.
- After `504 QBD_REQUEST_TIMEOUT`, any failure while waiting for the request (a poll answered `429`, `5xx` or `404`, a network error or a timeout) throws `RequestPendingException` with `RequestId`, `TimeoutError` (the 504, also the inner exception), `PollError` and `IdempotencyKey`. It never surfaces the poll's own retryable exception, which read as "safe to resend" and could duplicate a write. `RequestHandle.WaitAsync` follows the same rule.
- Waiting for a pending request stays inside the call's deadline (`TotalTimeout`, else `Timeout`): each poll, retry and backoff is cut off at the deadline.
- `IdempotencyKey` on every exception thrown for a write (generated or yours), on `ApiResponse<T>` and on `RequestHandle<T>`.
- A request that succeeded in QuickBooks but whose answer the API could not map (`request.error`, for example `QBD_RESPONSE_UNREADABLE` with outcome `applied`) throws that typed exception instead of a generic `DaapiException`.
- Exceptions thrown by `RequestHandle<T>.WaitAsync` and `ResultAsync` carry the handle's `IdempotencyKey`.
- A poll answer that arrives after the budget is not returned, even a settled one; the call throws `RequestPendingException` with that snapshot.

## 0.2.0

- `ClientOptions.DefaultHeaders`, and `TotalTimeout` on `ClientOptions` and `RequestOptions`: a time budget for a whole call, including retries and the wait for a pending request.
- A base URL ending in `/v1` (Conductor's form) no longer produces `/v1/v1/...`.
- `Pager<T>` requests the next page only when the iteration needs it, so a loop that stops early sends no extra QuickBooks query. While iterating items, a page held for more than 2 seconds makes the SDK request the next page in the background; `ListAllAsync()` always reads ahead.
- README: "Porting from Conductor".

## 0.1.0

First release, generated from API contract 1.0.0 (sha256 `1fc5496cc47b`).

- `DesktopAccountingApiClient` with typed async methods for all 275 operations, `ForEndUser`, per-call `RequestOptions` and `...WithResponseAsync` raw-response variants.
- Typed models and inputs: `decimal` money with preserved scale, `DateOnly` dates, `DateTimeOffset` timestamps, open enums as strings with constants, inputs that send only what you set (explicit `null` clears).
- `Pager<T>` (`IAsyncEnumerable<T>`) with one page of read-ahead and `CursorExpiredException` progress fields.
- Retries for network errors, `429` and retryable `5xx` with exponential backoff and `Retry-After`; an idempotency key per write, reused across retries; long polling after `504 QBD_REQUEST_TIMEOUT`.
- Async mode through `Enqueue` accessors returning `RequestHandle<T>`.
- Typed exceptions per error type with every error field, and `ErrorCodes` / `ErrorTypes` constants.
- Standard Webhooks verification (`WebhookVerifier`, `client.Webhooks`).
- Targets `netstandard2.0` and `net8.0`.
