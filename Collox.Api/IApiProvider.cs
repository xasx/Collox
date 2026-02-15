// Collox.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Collox.Api.IApiProvider
using System.Collections.Generic;
using System.Threading.Tasks;
using Collox.Api;
using Microsoft.Extensions.AI;

public interface IApiProvider : IPlugin
{
	Task<IEnumerable<string>> GetModelsAsync(ConnectionInfo connectionInfo);

	IChatClient GetClient(ConnectionInfo connectionInfo, string modelId);
}
