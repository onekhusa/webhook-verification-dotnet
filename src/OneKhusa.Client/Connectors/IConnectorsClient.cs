using OneKhusa.Client.Common;

namespace OneKhusa.Client.Connectors;

public interface IConnectorsClient
{
    /// <summary><c>GET /core/connectors/GetAll</c>: every connector, active or not.</summary>
    Task<OneKhusaResult<IReadOnlyList<Connector>>> GetAllAsync(CancellationToken cancellationToken = default);
}
