using OneKhusa.Client.Common;

namespace OneKhusa.Client.Authentication;

public interface IAccessTokenProvider
{
    /// <summary>Returns a cached token, or requests a new one when it is close to expiring.</summary>
    Task<OneKhusaResult<AccessToken>> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
