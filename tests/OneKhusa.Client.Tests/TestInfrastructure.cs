using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneKhusa.Client.DependencyInjection;

namespace OneKhusa.Client.Tests;

/// <summary>Answers requests by path and records what was sent.</summary>
internal sealed class FakeOneKhusaHandler : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();

    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

    public TimeSpan Delay { get; set; }

    public void Respond(string pathSuffix, HttpStatusCode status, string json) =>
        _routes[pathSuffix] = _ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public int CountRequests(string pathSuffix) => Requests.Count(r => r.Uri.AbsolutePath.EndsWith(pathSuffix, StringComparison.Ordinal));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Enqueue(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)), body));

        if (Delay > TimeSpan.Zero)
            await Task.Delay(Delay, cancellationToken);

        var route = _routes.FirstOrDefault(r => request.RequestUri!.AbsolutePath.EndsWith(r.Key, StringComparison.Ordinal));
        return route.Value?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, Dictionary<string, string> Headers, string? Body);

internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

internal static class TestServices
{
    public static readonly DateTimeOffset Now = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    public static Dictionary<string, string?> ValidSettings() => new()
    {
        ["OneKhusa:BaseUrl"] = "https://api.onekhusa.com/sandbox/v1",
        ["OneKhusa:ApiKey"] = "test-key",
        ["OneKhusa:ApiSecret"] = "test-secret",
        ["OneKhusa:OrganisationId"] = "org_001",
        ["OneKhusa:MerchantAccountNumber"] = "12345678",
    };

    public static ServiceProvider Build(FakeOneKhusaHandler handler, TimeProvider timeProvider, Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? ValidSettings()).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(timeProvider);
        services.AddOneKhusaClient(configuration);
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));
        return services.BuildServiceProvider();
    }

    public static string TokenJson(DateTimeOffset expiresOn) =>
        $$"""{"accessToken":"jwt-token","expiresOn":"{{expiresOn:O}}","expiryInMinutes":5}""";
}
