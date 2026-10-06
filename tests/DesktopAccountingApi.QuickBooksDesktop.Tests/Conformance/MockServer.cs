using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Conformance;

/// <summary>Locates the shared conformance fixtures (conformance/ at the repository root).</summary>
public static class Fixtures
{
    private static readonly Lazy<string> s_dir = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "conformance", "fixtures", "scenarios.json");
            if (File.Exists(candidate)) return Path.Combine(dir.FullName, "conformance");
            dir = dir.Parent;
        }
        throw new InvalidOperationException("conformance/fixtures/scenarios.json not found above " + AppContext.BaseDirectory);
    });

    private static readonly Lazy<JsonElement> s_scenarios = new(() => Load("scenarios.json"));
    private static readonly Lazy<JsonElement> s_webhooks = new(() => Load("webhooks.json"));
    private static readonly Lazy<JsonElement> s_apiKeys = new(() => Load("api-keys.json"));

    public static string Directory => s_dir.Value;
    public static JsonElement Scenarios => s_scenarios.Value;
    public static JsonElement Webhooks => s_webhooks.Value;
    public static JsonElement ApiKeys => s_apiKeys.Value;

    private static JsonElement Load(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Directory, "fixtures", name)));
        return doc.RootElement.Clone();
    }
}

/// <summary>Runs `node conformance/mock-server.mjs` for the test class and stops it afterwards.</summary>
public sealed class MockServer : IDisposable
{
    private readonly Process _process;

    public MockServer()
    {
        var script = Path.Combine(Fixtures.Directory, "mock-server.mjs");
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("--exit-on-stdin-close");
        _process = Process.Start(start) ?? throw new InvalidOperationException("could not start node");
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) Console.Error.WriteLine("[mock-server] " + e.Data);
        };
        _process.BeginErrorReadLine();
        var first = _process.StandardOutput.ReadLineAsync();
        if (!first.Wait(TimeSpan.FromSeconds(30)) || first.Result is null || !first.Result.StartsWith("MOCK_SERVER_URL=", StringComparison.Ordinal))
        {
            Dispose();
            throw new InvalidOperationException("mock server did not print MOCK_SERVER_URL");
        }
        Url = first.Result.Substring("MOCK_SERVER_URL=".Length).Trim();
        Control = new HttpClient { BaseAddress = new Uri(Url) };
    }

    public string Url { get; }

    public HttpClient Control { get; }

    public async Task ResetAsync(string scenario)
    {
        using var response = await Control.PostAsync("/_control/reset/" + scenario, null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonElement> VerifyAsync(string scenario)
    {
        var text = await Control.GetStringAsync("/_control/verify/" + scenario);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    public void Dispose()
    {
        Control?.Dispose();
        try
        {
            _process.StandardInput.Close();
            if (!_process.WaitForExit(3000)) _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
        _process.Dispose();
    }
}
