using System;
using System.Collections.Generic;
using System.Text.Json;
using DesktopAccountingApi.QuickBooksDesktop.Models;

namespace DesktopAccountingApi.QuickBooksDesktop;

#pragma warning disable CA1032 // Exceptions carry API data; the parameterless constructors would create invalid instances.

/// <summary>Base class of every exception the SDK throws. Thrown directly for client-side problems (missing or malformed API key, missing end user, invalid arguments, unparseable responses).</summary>
public class DaapiException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What went wrong.</param>
    public DaapiException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with an inner exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The underlying exception.</param>
    public DaapiException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The <c>Idempotency-Key</c> the SDK sent for the write that raised this exception (generated
    /// once per call unless you set <see cref="RequestOptions.IdempotencyKey"/>), else <c>null</c>.
    /// Resend a write whose outcome is <c>pending</c> or <c>unknown</c>, or that failed without a
    /// response, only with this key: the API then returns the original request.
    /// </summary>
    public string? IdempotencyKey { get; internal set; }
}

/// <summary>
/// The API answered with an error (any non-2xx response), or a request finished as failed,
/// canceled or <c>outcome_unknown</c>. Subclasses exist per error <see cref="Type"/>; an unknown
/// type stays this base class. Every field of the API error object is exposed directly.
/// </summary>
public class ApiException : DaapiException
{
    private static readonly IReadOnlyDictionary<string, string> s_noHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, JsonElement> s_noDetails = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    /// <summary>Creates the exception from an API error object.</summary>
    /// <param name="status">HTTP status of the response, or the error's <c>httpStatusCode</c> for request results.</param>
    /// <param name="error">The parsed error object, or <c>null</c> for a non-JSON error response.</param>
    /// <param name="headers">Response headers.</param>
    /// <param name="fallbackMessage">Message used when the error has none.</param>
    public ApiException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(BuildMessage(status, error, fallbackMessage))
    {
        Status = status;
        Error = error;
        Headers = headers ?? s_noHeaders;
        string? headerRequestId = null;
        if (headers is not null && headers.TryGetValue("Daapi-Request-Id", out var rid)) headerRequestId = rid;
        RequestId = string.IsNullOrEmpty(error?.RequestId) ? headerRequestId : error!.RequestId;
    }

    /// <summary>Copies another API exception (used to attach extra context such as cursor progress).</summary>
    /// <param name="other">The exception to copy.</param>
    protected ApiException(ApiException other)
        : base((other ?? throw new ArgumentNullException(nameof(other))).Message, other)
    {
        Status = other.Status;
        Error = other.Error;
        Headers = other.Headers;
        RequestId = other.RequestId;
        IdempotencyKey = other.IdempotencyKey;
    }

    private static string BuildMessage(int? status, Error? error, string? fallback)
    {
        if (!string.IsNullOrEmpty(error?.Message)) return error!.Message;
        if (!string.IsNullOrEmpty(error?.Code)) return error!.Code;
        return fallback ?? (status is { } s ? $"HTTP {s}" : "The API returned an error.");
    }

    /// <summary>
    /// The exception with its status, error code and request ID, for unhandled-exception output and
    /// loggers: <c>Type: 404 QBD_OBJECT_NOT_FOUND The QuickBooks object does not exist. (req_...)</c>,
    /// then the inner exception and stack trace. <see cref="Exception.Message"/> stays the API message.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString()
    {
        var text = new System.Text.StringBuilder(GetType().FullName).Append(": ");
        if (Status is { } status) text.Append(status.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
        if (!string.IsNullOrEmpty(Code)) text.Append(Code).Append(' ');
        text.Append(Message);
        if (!string.IsNullOrEmpty(RequestId)) text.Append(" (").Append(RequestId).Append(')');
        if (InnerException is not null) text.Append(" ---> ").Append(InnerException);
        if (StackTrace is not null) text.AppendLine().Append(StackTrace);
        return text.ToString();
    }

    /// <summary>The parsed API error object, or <c>null</c> when the response body was not a JSON error.</summary>
    public Error? Error { get; }

    /// <summary>HTTP status. For errors of finished requests (<see cref="RequestHandle{T}"/>, long polling) this is the error's <c>httpStatusCode</c>, which may be <c>null</c>.</summary>
    public int? Status { get; }

    /// <summary>Response headers (case-insensitive names). Empty for errors read from a request resource.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Error category, for example <c>INTEGRATION_CONNECTION_ERROR</c>. See <see cref="ErrorTypes"/>.</summary>
    public string? Type => Error?.Type;

    /// <summary>Stable error code, for example <c>QBD_MODAL_DIALOG_OPEN</c>. See <see cref="ErrorCodes"/>.</summary>
    public string? Code => Error?.Code;

    /// <summary>A message that is safe to show to the end user.</summary>
    public string? UserFacingMessage => Error?.UserFacingMessage;

    /// <summary>HTTP status code recorded in the error object (<c>null</c> for codes that only appear on request resources).</summary>
    public int? HttpStatusCode => Error?.HttpStatusCode;

    /// <summary>Native QuickBooks code when one exists (a qbXML statusCode or an HRESULT).</summary>
    public string? IntegrationCode => Error?.IntegrationCode;

    /// <summary>The request ID: the error's <c>requestId</c>, else the <c>Daapi-Request-Id</c> response header. Include it when you contact support.</summary>
    public string? RequestId { get; }

    /// <summary>Why this error happens (the error's <c>cause</c> field; not the .NET exception chain).</summary>
    public string? Cause => Error?.Cause;

    /// <summary>Ordered actions that resolve the error, each with the responsible actor.</summary>
    public IReadOnlyList<ErrorFix> Fixes => Error?.Fixes ?? Array.Empty<ErrorFix>();

    /// <summary>Documentation section for this code.</summary>
    public string? DocsUrl => Error?.DocsUrl;

    /// <summary>Whether repeating the identical request (with the same idempotency key) can succeed.</summary>
    public bool? Retryable => Error?.Retryable;

    /// <summary>Whether a write took effect: <c>applied</c>, <c>not_applied</c>, <c>pending</c>, <c>unknown</c> or <c>not_applicable</c>.</summary>
    public string? Outcome => Error?.Outcome;

    /// <summary>The request field, query parameter or header the error refers to.</summary>
    public string? Param => Error?.Param;

    /// <summary>Code-specific details.</summary>
    public IReadOnlyDictionary<string, JsonElement> Details => Error?.Details ?? s_noDetails;
}

/// <summary><c>INVALID_REQUEST_ERROR</c>: the request is malformed or not allowed in the current state. Fix the request before retrying.</summary>
public class InvalidRequestException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public InvalidRequestException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }

    /// <inheritdoc cref="ApiException(ApiException)"/>
    protected InvalidRequestException(ApiException other)
        : base(other)
    {
    }
}

/// <summary>
/// <c>410 CURSOR_EXPIRED</c>: a pagination cursor expired while iterating (idle window passed,
/// QuickBooks session ended or restarted, iterator evicted). The SDK never restarts a list
/// silently: restart the list with <c>UpdatedAfter</c> set to a watermark (for example the
/// <see cref="LastUpdatedAt"/> of the last item you processed) and skip IDs you already have.
/// </summary>
public sealed class CursorExpiredException : InvalidRequestException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public CursorExpiredException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
        PagesServed = DetailInt("pagesServed") ?? 0;
    }

    internal CursorExpiredException(ApiException other, int itemsYielded, int pagesDelivered, string? lastId, string? lastUpdatedAt)
        : base(other)
    {
        ItemsYielded = itemsYielded;
        PagesServed = DetailInt("pagesServed") ?? pagesDelivered;
        LastId = lastId;
        LastUpdatedAt = lastUpdatedAt;
    }

    private int? DetailInt(string name) =>
        Details.TryGetValue(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;

    /// <summary>Items the iterator yielded before the cursor expired.</summary>
    public int ItemsYielded { get; }

    /// <summary>Pages the server served for this cursor (<c>details.pagesServed</c>, else the pages this iterator received).</summary>
    public int PagesServed { get; }

    /// <summary>ID of the last item yielded, or <c>null</c>.</summary>
    public string? LastId { get; }

    /// <summary><c>updatedAt</c> of the last item yielded, exactly as the API sent it, or <c>null</c>.</summary>
    public string? LastUpdatedAt { get; }

    /// <summary>Why the cursor expired: <c>idle_timeout</c>, <c>session_ended</c>, <c>quickbooks_restarted</c> or <c>evicted</c>.</summary>
    public string? Reason => Details.TryGetValue("reason", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary><c>AUTHENTICATION_ERROR</c>: the API key is missing, invalid or revoked.</summary>
public sealed class AuthenticationException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public AuthenticationException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>PERMISSION_ERROR</c>: the key may not perform this operation or access this end user.</summary>
public sealed class PermissionException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public PermissionException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>BILLING_ERROR</c>: billing must be set up or updated before this request can run.</summary>
public sealed class BillingException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public BillingException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>RATE_LIMIT_ERROR</c>: too many requests. The SDK already retried with backoff.</summary>
public sealed class RateLimitException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public RateLimitException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>INTEGRATION_CONNECTION_ERROR</c>: QuickBooks Desktop could not be reached or used (Web Connector not running, company file closed, modal dialog open, timeout).</summary>
public sealed class IntegrationConnectionException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public IntegrationConnectionException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>INTEGRATION_ERROR</c>: QuickBooks rejected the request (object not found, duplicate name, validation failure).</summary>
public sealed class IntegrationException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public IntegrationException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>OUTCOME_UNKNOWN_ERROR</c>: a write was sent but its result is unknown. The SDK never retries it; check the record or wait for <c>request.outcome_resolved</c>.</summary>
public sealed class OutcomeUnknownException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public OutcomeUnknownException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary><c>INTERNAL_ERROR</c>: an unexpected server error.</summary>
public sealed class InternalException : ApiException
{
    /// <inheritdoc cref="ApiException(int?, Error?, IReadOnlyDictionary{string, string}?, string?)"/>
    public InternalException(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallbackMessage = null)
        : base(status, error, headers, fallbackMessage)
    {
    }
}

/// <summary>No response arrived: connection failure, reset or dropped connection. Thrown after the SDK's retries are exhausted.</summary>
public class ApiConnectionException : DaapiException
{
    /// <inheritdoc cref="DaapiException(string, Exception?)"/>
    public ApiConnectionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The client-side timeout (<see cref="ClientOptions.Timeout"/> or <see cref="RequestOptions.Timeout"/>) elapsed before a response arrived.</summary>
public sealed class ApiTimeoutException : ApiConnectionException
{
    /// <inheritdoc cref="DaapiException(string, Exception?)"/>
    public ApiTimeoutException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// The request is still queued or running when the SDK stopped waiting (after a
/// <c>504 QBD_REQUEST_TIMEOUT</c> or in <see cref="RequestHandle{T}.WaitAsync"/>). It was not
/// resubmitted. The SDK throws this, never the exception of a failed poll, whenever it cannot learn
/// the request's final state: a poll that failed with 429, 5xx, 404 or a network error says nothing
/// about the write. Collect the result later with <c>client.Requests.RetrieveAsync(RequestId)</c>
/// or a webhook, or resend only with <see cref="DaapiException.IdempotencyKey"/>.
/// </summary>
public sealed class RequestPendingException : DaapiException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="requestId">The pending request's ID.</param>
    /// <param name="request">The last request snapshot, if one was fetched.</param>
    public RequestPendingException(string requestId, Request? request)
        : this(requestId, request, null, null, null)
    {
    }

    /// <summary>Creates the exception with the error that started the wait and the poll failure that ended it.</summary>
    /// <param name="requestId">The pending request's ID.</param>
    /// <param name="request">The last request snapshot, if one was fetched.</param>
    /// <param name="timeoutError">The <c>504 QBD_REQUEST_TIMEOUT</c> that started the wait (also the inner exception), if any.</param>
    /// <param name="pollError">The failed poll that ended the wait, if any.</param>
    /// <param name="idempotencyKey">The write's <c>Idempotency-Key</c>, if any.</param>
    public RequestPendingException(string requestId, Request? request, ApiException? timeoutError, DaapiException? pollError, string? idempotencyKey)
        : base(
            $"Request {requestId} has not finished (status {request?.Status ?? "pending"}): {(pollError is null ? "the time budget ended" : "checking it failed (" + pollError.Message + ")")}. It was not resubmitted; retrieve it later with client.Requests.RetrieveAsync(\"{requestId}\")"
                + (idempotencyKey is null ? "." : $" or resend it only with Idempotency-Key {idempotencyKey}."),
            (Exception?)timeoutError ?? pollError)
    {
        RequestId = requestId;
        Request = request;
        TimeoutError = timeoutError;
        PollError = pollError;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>The <c>504 QBD_REQUEST_TIMEOUT</c> exception (with <c>details.diagnosis</c>) that started the wait, if any.</summary>
    public ApiException? TimeoutError { get; }

    /// <summary>The failed poll that ended the wait, if any. It says nothing about the write: never resend with a new key because of it.</summary>
    public DaapiException? PollError { get; }

    /// <summary>The pending request's ID (<c>req_...</c>).</summary>
    public string RequestId { get; }

    /// <summary>The last request snapshot, if one was fetched.</summary>
    public Request? Request { get; }
}

/// <summary>A webhook failed verification: bad or missing signature headers, wrong secret, timestamp outside the tolerance, or a body that is not an event.</summary>
public sealed class WebhookVerificationException : DaapiException
{
    /// <inheritdoc cref="DaapiException(string)"/>
    public WebhookVerificationException(string message)
        : base(message)
    {
    }
}

#pragma warning restore CA1032

internal static class ErrorFactory
{
    private sealed class Envelope
    {
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public Error? Error { get; set; }
    }

    public static ApiException FromResponse(int status, IReadOnlyDictionary<string, string> headers, string body)
    {
        Error? error = null;
        try
        {
            if (body.Length > 0 && body.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                error = JsonSerializer.Deserialize<Envelope>(body, DesktopAccountingApiJson.Options)?.Error;
            }
        }
        catch (JsonException)
        {
            error = null;
        }
        return Create(status, error, headers, error is null ? $"HTTP {status}: the response is not a JSON error object." : null);
    }

    public static ApiException Create(int? status, Error? error, IReadOnlyDictionary<string, string>? headers, string? fallback = null)
    {
        if (error is null) return new ApiException(status, null, headers, fallback);
        return error.Type switch
        {
            ErrorTypes.InvalidRequestError when error.Code == ErrorCodes.CursorExpired => new CursorExpiredException(status, error, headers, fallback),
            ErrorTypes.InvalidRequestError => new InvalidRequestException(status, error, headers, fallback),
            ErrorTypes.AuthenticationError => new AuthenticationException(status, error, headers, fallback),
            ErrorTypes.PermissionError => new PermissionException(status, error, headers, fallback),
            ErrorTypes.BillingError => new BillingException(status, error, headers, fallback),
            ErrorTypes.RateLimitError => new RateLimitException(status, error, headers, fallback),
            ErrorTypes.IntegrationConnectionError => new IntegrationConnectionException(status, error, headers, fallback),
            ErrorTypes.IntegrationError => new IntegrationException(status, error, headers, fallback),
            ErrorTypes.OutcomeUnknownError => new OutcomeUnknownException(status, error, headers, fallback),
            ErrorTypes.InternalError => new InternalException(status, error, headers, fallback),
            _ => new ApiException(status, error, headers, fallback),
        };
    }
}
