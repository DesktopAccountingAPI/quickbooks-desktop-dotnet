# Changelog

## 0.1.0

First release, generated from API contract 1.0.0 (sha256 `1cc3058cecb5`).

- `DesktopAccountingApiClient` with typed async methods for all 275 operations, `ForEndUser`, per-call `RequestOptions` and `...WithResponseAsync` raw-response variants.
- Typed models and inputs: `decimal` money with preserved scale, `DateOnly` dates, `DateTimeOffset` timestamps, open enums as strings with constants, inputs that send only what you set (explicit `null` clears).
- `Pager<T>` (`IAsyncEnumerable<T>`) with one page of read-ahead and `CursorExpiredException` progress fields.
- Retries for network errors, `429` and retryable `5xx` with exponential backoff and `Retry-After`; an idempotency key per write, reused across retries; long polling after `504 QBD_REQUEST_TIMEOUT`.
- Async mode through `Enqueue` accessors returning `RequestHandle<T>`.
- Typed exceptions per error type with every error field, and `ErrorCodes` / `ErrorTypes` constants.
- Standard Webhooks verification (`WebhookVerifier`, `client.Webhooks`).
- Targets `netstandard2.0` and `net8.0`.
