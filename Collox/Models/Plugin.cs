using Collox.Api;

namespace Collox.Models;

public class Plugin
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Version { get; init; }
    public string Author { get; init; }
    public string Description { get; init; }

    public IPlugin InitPlugin { get; set; }

    public Dictionary<string, Lazy<IApiProvider>> ApiProvidersByName { get; init; } = new();
}

public record ApiProviderDescription(string Name, string Id );
