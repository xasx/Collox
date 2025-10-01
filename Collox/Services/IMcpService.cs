using ModelContextProtocol.Client;

namespace Collox.Services;

public interface IMcpService
{
    ValueTask<IList<McpClientTool>> GetTools();
}
