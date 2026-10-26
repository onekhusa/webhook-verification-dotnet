using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OneKhusa.Client.Authentication;

namespace OneKhusa.Client.Tests;

public class AccessTokenProviderTests
{
    private const string TokenPath = "/sandbox/v1/account/getAccessToken";

    private readonly FakeOneKhusaHandler _handler = new();
    private readonly ManualTimeProvider _time = new(TestServices.Now);

    private IAccessTokenProvider CreateProvider(Dictionary<string, string?>? settings = null) =>
        TestServices.Build(_handler, _time, settings).GetRequiredService<IAccessTokenProvider>();

    [Fact]
    public async Task Sends_credentials_to_the_token_endpoint()
    {
        _handler.Respond(TokenPath, HttpStatusCode.OK, TestServices.TokenJson(TestServices.Now.AddMinutes(5)));

        var result = await CreateProvider().GetAccessTokenAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.Value);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.onekhusa.com/sandbox/v1/account/getAccessToken", request.Uri.ToString());

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("test-key", body.RootElement.GetProperty("apiKey").GetString());
        Assert.Equal("test-secret", body.RootElement.GetProperty("apiSecret").GetString());
        Assert.Equal("org_001", body.RootElement.GetProperty("organisationId").GetString());
        Assert.Equal(12345678, body.RootElement.GetProperty("merchantAccountNumber").GetInt32());
    }

    [Fact]
    public async Task Reuses_the_cached_token_until_close_to_expiry()
    {
        _handler.Respond(TokenPath, HttpStatusCode.OK, TestServices.TokenJson(TestServices.Now.AddMinutes(5)));
        var provider = CreateProvider();

        await provider.GetAccessTokenAsync();
        _time.Advance(TimeSpan.FromMinutes(3));
        await provider.GetAccessTokenAsync();
        Assert.Equal(1, _handler.CountRequests(TokenPath));

        // Inside the 60 second refresh margin.
        _time.Advance(TimeSpan.FromSeconds(61));
        await provider.GetAccessTokenAsync();
        Assert.Equal(2, _handler.CountRequests(TokenPath));
    }

    [Fact]
    public async Task Concurrent_callers_share_one_token_request()
    {
        _handler.Respond(TokenPath, HttpStatusCode.OK, TestServices.TokenJson(TestServices.Now.AddMinutes(5)));
        _handler.Delay = TimeSpan.FromMilliseconds(100);
        var provider = CreateProvider();

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => provider.GetAccessTokenAsync()));

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(1, _handler.CountRequests(TokenPath));
    }

    [Fact]
    public async Task Missing_configuration_fails_without_calling_the_api()
    {
        var settings = TestServices.ValidSettings();
        settings["OneKhusa:ApiKey"] = "";

        var result = await CreateProvider(settings).GetAccessTokenAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("ApiKey", result.Error.Message);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Rejected_credentials_are_returned_as_an_error_and_not_cached()
    {
        _handler.Respond(TokenPath, HttpStatusCode.Unauthorized, """{"errorMessages":["Invalid API credentials"]}""");
        var provider = CreateProvider();

        var first = await provider.GetAccessTokenAsync();
        await provider.GetAccessTokenAsync();

        Assert.False(first.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, first.Error.StatusCode);
        Assert.Contains("Invalid API credentials", first.Error.Message);
        Assert.Equal(2, _handler.CountRequests(TokenPath));
    }

    [Fact]
    public async Task Problem_details_error_shows_detail_and_error_code()
    {
        // Real sandbox response for credentials that do not match.
        _handler.Respond(TokenPath, HttpStatusCode.BadRequest, """
            {"type":"https://httpstatuses.com/400","title":"Validation Error","status":400,
             "detail":"Failed to retrieve access token","instance":"/sandbox/v1/account/getAccessToken","errorCode":"E900"}
            """);

        var result = await CreateProvider().GetAccessTokenAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("OneKhusa returned HTTP 400: Failed to retrieve access token (E900)", result.Error.Message);
    }

    [Fact]
    public void Token_is_not_exposed_by_ToString()
    {
        var token = new AccessToken("super-secret-jwt", TestServices.Now);

        Assert.DoesNotContain("super-secret-jwt", token.ToString());
    }
}
