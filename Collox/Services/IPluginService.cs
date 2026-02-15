using Collox.Models;

namespace Collox.Services;

public interface IPluginService
{
    Task LoadPluginsAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<Plugin> GetLoadedPlugins();
    Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default);
}
