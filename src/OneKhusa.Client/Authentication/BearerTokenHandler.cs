using System.Net.Http.Headers;

namespace OneKhusa.Client.Authentication;

/// <summary>Adds <c>Authorization: Bearer &lt;token&gt;</c> to every API request.</summary>
internal sealed class BearerTokenHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!token.IsSuccess)
            throw new OneKhusaAuthenticationException(token.Error);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value.Value);
        return await base.SendAsync(request, cancellationToken);
    }
}
