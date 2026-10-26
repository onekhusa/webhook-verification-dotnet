using Microsoft.Extensions.Logging;
using OneKhusa.Client.Common;

namespace OneKhusa.Client.Connectors;

internal sealed class ConnectorsClient(HttpClient httpClient, ILogger<ConnectorsClient> logger) : IConnectorsClient
{
    public async Task<OneKhusaResult<IReadOnlyList<Connector>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "core/connectors/GetAll");
        var result = await OneKhusaHttp.SendAsync<List<Connector>>(httpClient, request, logger, cancellationToken);

        return result.IsSuccess
            ? OneKhusaResult<IReadOnlyList<Connector>>.Success(result.Value)
            : OneKhusaResult<IReadOnlyList<Connector>>.Failure(result.Error);
    }
}
