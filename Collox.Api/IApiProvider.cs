using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collox.Api;

public interface IApiProvider : IPlugin
{

    Task<IEnumerable<string>> GetModelsAsync(ConnectionInfo connectionInfo);

    IChatClient GetClient(ConnectionInfo connectionInfo, string modelId);
}

public record ConnectionInfo(string ApiKey, string EndpointUrl);

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ApiProviderNameAttribute : System.Attribute
{
    public string Name { get; }

    public string Id { get; }

    public ApiProviderNameAttribute(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
