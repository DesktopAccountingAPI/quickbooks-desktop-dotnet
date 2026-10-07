# Changelog

## 0.2.0

- `ClientOptions.DefaultHeaders`, and `TotalTimeout` on `ClientOptions` and `RequestOptions`: a time budget for a whole call, including retries and the wait for a pending request.
- A base URL ending in `/v1` (Conductor's form) no longer produces `/v1/v1/...`.
- `Pager<T>` requests the next page only when the iteration needs it, so a loop that stops early sends no extra QuickBooks query. While iterating items, a page held for more than 2 seconds makes the SDK request the next page in the background; `ListAllAsync()` always reads ahead.
- README: "Porting from Conductor".

## 0.1.0

First release, generated from API contract 1.0.0 (sha256 `b5774d24bc81`).

- `DesktopAccountingApiClient` with typed async methods for all 275 operations, `ForEndUser`, per-call `RequestOptions` and `...WithResponseAsync` raw-response variants.
- Typed models and inputs: `decimal` money with preserved scale, `DateOnly` dates, `DateTimeOffset` timestamps, open enums as strings with constants, inputs that send only what you set (explicit `null` clears).
- `Pager<T>` (`IAsyncEnumerable<T>`) with one page of read-ahead and `CursorExpiredException` progress fields.
- Retries for network errors, `429` and retryable `5xx` with exponential backoff and `Retry-After`; an idempotency key per write, reused across retries; long polling after `504 QBD_REQUEST_TIMEOUT`.
- Async mode through `Enqueue` accessors returning `RequestHandle<T>`.
- Typed exceptions per error type with every error field, and `ErrorCodes` / `ErrorTypes` constants.
- Standard Webhooks verification (`WebhookVerifier`, `client.Webhooks`).
- Targets `netstandard2.0` and `net8.0`.
