using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Collox.Api;

public interface IApiProvider
{
    Task<IEnumerable<string>> GetModelsAsync(ConnectionInfo connectionInfo);

    IChatClient GetClient(ConnectionInfo connectionInfo, string modelId);
}
