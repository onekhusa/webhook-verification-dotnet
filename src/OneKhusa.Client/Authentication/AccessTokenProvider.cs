using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneKhusa.Client.Common;
using OneKhusa.Client.Configuration;

namespace OneKhusa.Client.Authentication;

/// <summary>
/// Calls <c>POST /account/getAccessToken</c> and caches the token until shortly before it expires.
/// Registered as a singleton so every API call shares one token.
/// </summary>
internal sealed class AccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OneKhusaOptions> options,
    TimeProvider timeProvider,
    ILogger<AccessTokenProvider> logger) : IAccessTokenProvider
{
    internal const string HttpClientName = "OneKhusa.Authentication";

    // Refresh early so a token never expires between being handed out and reaching OneKhusa.
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile AccessToken? _cachedToken;

    public async Task<OneKhusaResult<AccessToken>> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsFresh(_cachedToken))
            return OneKhusaResult<AccessToken>.Success(_cachedToken);

        // Only one caller refreshes; the others wait and reuse its token.
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (IsFresh(_cachedToken))
                return OneKhusaResult<AccessToken>.Success(_cachedToken);

            var result = await RequestTokenAsync(cancellationToken);
            if (result.IsSuccess)
                _cachedToken = result.Value;
            return result;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsFresh([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] AccessToken? token) =>
        token is not null && timeProvider.GetUtcNow() < token.ExpiresOn - RefreshMargin;

    private async Task<OneKhusaResult<AccessToken>> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var configurationErrors = settings.GetValidationErrors();
        if (configurationErrors.Count > 0)
            return OneKhusaResult<AccessToken>.Failure(new OneKhusaError("OneKhusa is not configured: " + string.Join(" ", configurationErrors)));

        using var request = new HttpRequestMessage(HttpMethod.Post, "account/getAccessToken")
        {
            Content = JsonContent.Create(new GetAccessTokenRequest(
                settings.ApiKey,
                settings.ApiSecret,
                settings.OrganisationId,
                settings.MerchantAccountNumber)),
        };

        var response = await OneKhusaHttp.SendAsync<GetAccessTokenResponse>(
            httpClientFactory.CreateClient(HttpClientName), request, logger, cancellationToken);
        if (!response.IsSuccess)
            return OneKhusaResult<AccessToken>.Failure(response.Error);

        var body = response.Value;
        if (string.IsNullOrEmpty(body.AccessToken))
            return OneKhusaResult<AccessToken>.Failure(new OneKhusaError("OneKhusa did not return an access token."));

        var expiresOn = body.ExpiresOn ?? timeProvider.GetUtcNow().AddMinutes(body.ExpiryInMinutes is > 0 ? body.ExpiryInMinutes.Value : 5);
        logger.LogInformation("Obtained OneKhusa access token, expires {ExpiresOn:O}", expiresOn);

        return OneKhusaResult<AccessToken>.Success(new AccessToken(body.AccessToken, expiresOn));
    }

    private sealed record GetAccessTokenRequest(
        [property: JsonPropertyName("apiKey")] string ApiKey,
        [property: JsonPropertyName("apiSecret")] string ApiSecret,
        [property: JsonPropertyName("organisationId")] string OrganisationId,
        [property: JsonPropertyName("merchantAccountNumber")] int MerchantAccountNumber);

    private sealed record GetAccessTokenResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("expiresOn")] DateTimeOffset? ExpiresOn,
        [property: JsonPropertyName("expiryInMinutes")] int? ExpiryInMinutes);
}
