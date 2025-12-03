using Collox.Api;

namespace Collox.Services;

public interface IPluginService
{
    Task LoadPluginsAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<IPlugin> GetLoadedPlugins();
    Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default);
}
