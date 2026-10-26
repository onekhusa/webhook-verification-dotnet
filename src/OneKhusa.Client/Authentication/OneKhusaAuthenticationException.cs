using OneKhusa.Client.Common;

namespace OneKhusa.Client.Authentication;

/// <summary>
/// Raised by <see cref="BearerTokenHandler"/> when no token could be obtained. The API clients turn it
/// back into a failed <see cref="OneKhusaResult{T}"/>, so callers never see it.
/// </summary>
internal sealed class OneKhusaAuthenticationException(OneKhusaError error) : Exception(error.Message)
{
    public OneKhusaError Error { get; } = error;
}
