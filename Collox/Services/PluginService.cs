using Collox.Api;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace Collox.Services;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}

public class PluginService : IPluginService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PluginService> _logger;
    private readonly Dictionary<string, (PluginAttribute Plugin, PluginLoadContext Context)> _loadedPlugins = new();
    private readonly string _pluginsDirectory;

    public PluginService(IServiceProvider services, ILogger<PluginService> logger)
    {
        _services = services;
        _logger = logger;
        _pluginsDirectory = Path.Combine(Settings.BaseFolder, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading plugins from: {PluginsDirectory}", _pluginsDirectory);

        foreach (var pluginPath in Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var loadContext = new PluginLoadContext(pluginPath);
                var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

                

                // Test assembly for pluginattribute
                var pluginAttribute = assembly.GetCustomAttribute<PluginAttribute>();

                if (pluginAttribute is null)
                {
                    _logger.LogWarning("No plugin descriptor found in assembly: {PluginPath}", pluginPath);
                    _logger.LogInformation("Unloading context for assembly: {PluginPath}", pluginPath);
                    loadContext.Unload();
                    continue;
                }
                //enumerate plugin interfaces in the assembly
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                    await plugin.InitializeAsync(_services, cancellationToken);

                    _loadedPlugins[plugin.Name] = (plugin, loadContext);
                    _logger.LogInformation("Loaded plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from: {PluginPath}", pluginPath);
            }
        }
    }

    public IReadOnlyCollection<IPlugin> GetLoadedPlugins() =>
        _loadedPlugins.Values.Select(p => p.Plugin).ToList();

    public async Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var pluginInfo))
        {
            await pluginInfo.Plugin.ShutdownAsync(cancellationToken);
            pluginInfo.Context.Unload();
            _loadedPlugins.Remove(pluginName);
            _logger.LogInformation("Unloaded plugin: {PluginName}", pluginName);
        }
    }
}
