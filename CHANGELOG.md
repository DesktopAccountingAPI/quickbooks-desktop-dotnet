# Changelog

## Unreleased

## 0.5.3 (2026-10-09)

- Released in lockstep with the other Desktop Accounting API packages; no entries for this package.

## 0.5.2 (2026-10-09)

- Released in lockstep with the other Desktop Accounting API packages; no entries for this package.

## 0.5.1 (2026-10-09)

- `ApiResponse<T>.RequestId` and `Page<T>.RequestId` after a long-polled call are the ID of the request that produced the result (the 504's `details.requestId`), which `client.Requests.RetrieveAsync()` finds. They were the last poll's ID, which answers `404`. The poll's own ID stays in `Headers["Daapi-Request-Id"]`.
- The README says that required input properties are checked before the request is sent, not by the compiler: a missing one, such as `FiscalYear` on `ReportBudgetSummaryParams`, throws `DaapiException` and nothing is sent.
- `QBD_OBJECT_IN_USE` errors (QuickBooks status 3175 or 3176, a record open for editing or locked by another user) carry `details.diagnosis` with the new diagnosis cause `record_in_use`: close the edit window in QuickBooks, then retry. The cause's `details.lockedBy` names the user when QuickBooks does. The request's `diagnosis` has the same cause.

## 0.5.0 (2026-10-09)

- A read that hits the server timeout (`504 QBD_REQUEST_TIMEOUT` with outcome `not_applicable`) is long-polled like a write: the SDK waits on `GET /v1/requests/{id}` until the call's deadline and returns the result or throws `RequestPendingException`, instead of throwing the 504 at once. The rule is the same in every SDK: HTTP 504, code `QBD_REQUEST_TIMEOUT` and a `details.requestId`, whatever the outcome. Such a 504 is never retried.
- **Breaking:** `ApiResponse<T>.Warnings` is the number of QuickBooks warnings (`int`, 0 when the `Daapi-Warnings` header is absent), not the header text. Read `Headers["Daapi-Warnings"]` for the raw value.
- **Breaking (types):** Response prices, rates and percentages (for example `QbdInvoiceLine.rate`, `QbdSalesOrPurchaseDetail.price`, `ratePercent`) carry the same decimal pattern as their inputs and as amounts, so they are `decimal` instead of `string`.
- The README documents resuming a list from a stored `NextCursor` (`new InvoiceListParams {{ Cursor = savedCursor }}`), now covered by the cross-language conformance suite.

## 0.4.0 (2026-10-08)

- Warning code `QBD_PERSONAL_DATA_WITHHELD`: employee responses carry one warning per `Ssn` that QuickBooks withheld because the integration may not read personal data (`Ssn` stays `null`; `Daapi-Warnings` counts the warnings).
- `PersonalDataAccess` on an end user's integration connections: `allowed`, `denied` or `unknown`.
- A failed passthrough's error `Details["requests"]` lists the status code, severity and message of every qbXML message, so a message skipped by `stopOnError` is visible.

## 0.3.0 (2026-10-08)

- **Breaking:** `Qbd.Reports.BudgetSummaryAsync` now takes `ReportBudgetSummaryParams` as a required argument, and `FiscalYear` is required in it. The API always rejected a budget report without it (`400 INVALID_PARAMETER`, `param: "fiscalYear"`), so no working call changes behavior; a call without the params no longer compiles. A params object without `FiscalYear` still compiles and throws `DaapiException` before sending, instead of the API error. Set `FiscalYear`, for example `new ReportBudgetSummaryParams { ReportType = ..., FiscalYear = 2026 }`.
- `WebhookEventTypes.ConnectionCompanyFileRemarked` (`connection.company_file_remarked`): the marker that identifies a connection's company file was created, written back after the file lost it (for example a restored backup) or adopted from the file; `data.reason` is `marker_created`, `marker_restored` or `marker_adopted`.
- After `504 QBD_REQUEST_TIMEOUT`, any failure while waiting for the request (a poll answered `429`, `5xx` or `404`, a network error or a timeout) throws `RequestPendingException` with `RequestId`, `TimeoutError` (the 504, also the inner exception), `PollError` and `IdempotencyKey`. It never surfaces the poll's own retryable exception, which read as "safe to resend" and could duplicate a write. `RequestHandle.WaitAsync` follows the same rule.
- Waiting for a pending request stays inside the call's deadline (`TotalTimeout`, else `Timeout`): each poll, retry and backoff is cut off at the deadline.
- `IdempotencyKey` on every exception thrown for a write (generated or yours), on `ApiResponse<T>` and on `RequestHandle<T>`.
- A request that succeeded in QuickBooks but whose answer the API could not map (`request.error`, for example `QBD_RESPONSE_UNREADABLE` with outcome `applied`) throws that typed exception instead of a generic `DaapiException`.
- Exceptions thrown by `RequestHandle<T>.WaitAsync` and `ResultAsync` carry the handle's `IdempotencyKey`.
- A poll answer that arrives after the budget is not returned, even a settled one; the call throws `RequestPendingException` with that snapshot.

## 0.2.1 (2026-10-07)

Generated from API contract sha256 `b5774d24bc81`. Documentation only; no API surface change.

- `updatedAt` and `revisionNumber` descriptions say that QuickBooks changes them at most once per second: an incremental sync should overlap `updatedAfter` and deduplicate by `id` and `revisionNumber`.
- The fixes for status 3261 (`QBD_INSUFFICIENT_PERMISSION`) name the personal-data checkbox in QuickBooks and what to do when it is gray: send the end user a new setup link and choose "Enable payroll access".
- Item sites document what QuickBooks returns without Advanced Inventory: an empty list, and `404 QBD_OBJECT_NOT_FOUND` from retrieve, rather than an error.

## 0.2.0 (2026-10-07)

- `ClientOptions.DefaultHeaders`, and `TotalTimeout` on `ClientOptions` and `RequestOptions`: a time budget for a whole call, including retries and the wait for a pending request.
- A base URL ending in `/v1` (Conductor's form) no longer produces `/v1/v1/...`.
- `Pager<T>` requests the next page only when the iteration needs it, so a loop that stops early sends no extra QuickBooks query. While iterating items, a page held for more than 2 seconds makes the SDK request the next page in the background; `ListAllAsync()` always reads ahead.
- README: "Porting from Conductor".

## 0.1.1 (2026-10-06)

Generated from API contract sha256 `1cc3058cecb5`, the same contract as 0.1.0. No API surface change.

- The README is rewritten: install with exact package coordinates, authentication, a quickstart, common workflows, errors, async requests and webhooks, versioning and support. Every code sample in it is compiled against the package before release, and the quickstart runs against a mock server.

## 0.1.0 (2026-10-06)

First release, generated from API contract 1.0.0 (sha256 `1cc3058cecb5`).

- `DesktopAccountingApiClient` with typed async methods for all 275 operations, `ForEndUser`, per-call `RequestOptions` and `...WithResponseAsync` raw-response variants.
- Typed models and inputs: `decimal` money with preserved scale, `DateOnly` dates, `DateTimeOffset` timestamps, open enums as strings with constants, inputs that send only what you set (explicit `null` clears).
- `Pager<T>` (`IAsyncEnumerable<T>`) with one page of read-ahead and `CursorExpiredException` progress fields.
- Retries for network errors, `429` and retryable `5xx` with exponential backoff and `Retry-After`; an idempotency key per write, reused across retries; long polling after `504 QBD_REQUEST_TIMEOUT`.
- Async mode through `Enqueue` accessors returning `RequestHandle<T>`.
- Typed exceptions per error type with every error field, and `ErrorCodes` / `ErrorTypes` constants.
- Standard Webhooks verification (`WebhookVerifier`, `client.Webhooks`).
- Targets `netstandard2.0` and `net8.0`.
