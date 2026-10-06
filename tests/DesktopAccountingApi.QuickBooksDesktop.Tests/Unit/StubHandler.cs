using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Unit;

/// <summary>Records requests and answers them from a queue (or throws to simulate network errors).</summary>
public sealed class StubHandler : HttpMessageHandler
{
    public const string Key = "sk_test_Conformance0Key0For0SDK0Tests000010nQFLR";

    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = new();

    public StubHandler Respond(int status, string body, params (string Name, string Value)[] headers)
    {
        _responses.Enqueue(_ =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            foreach (var (name, value) in headers) response.Headers.TryAddWithoutValidation(name, value);
            return response;
        });
        return this;
    }

    public StubHandler Fail()
    {
        _responses.Enqueue(_ => throw new HttpRequestException("connection reset"));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        if (_responses.Count == 0) throw new InvalidOperationException("unexpected request " + request.RequestUri);
        return _responses.Dequeue()(request);
    }

    public static string? Header(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;

    /// <summary>A client on this handler with instant retries.</summary>
    public DesktopAccountingApiClient Client(string? endUserId = "eu_test", int maxRetries = 2, List<TimeSpan>? delays = null)
    {
        var client = new DesktopAccountingApiClient(new ClientOptions
        {
            ApiKey = Key,
            BaseUrl = "https://api.example.test/base/",
            EndUserId = endUserId,
            MaxRetries = maxRetries,
            HttpMessageHandler = this,
        });
        client.Core.Delay = (d, _) =>
        {
            delays?.Add(d);
            return Task.CompletedTask;
        };
        return client;
    }
}
