using Collox.Models;

namespace Collox.Services;

public interface IPluginService
{
    /// <summary>
    /// Phase 1: Discover and load plugin assemblies without initializing them.
    /// </summary>
    Task LoadPluginsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 2: Initialize all loaded plugins that implement IPlugin.
    /// </summary>
    Task InitializePluginsAsync(CancellationToken cancellationToken = default);

    IReadOnlyCollection<Plugin> GetLoadedPlugins();
    Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default);
}
