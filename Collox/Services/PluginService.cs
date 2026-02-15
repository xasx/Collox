using Collox.Api;
using Collox.Models;
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
    private readonly Dictionary<string, (Plugin Plugin, PluginLoadContext Context)> _loadedPlugins = new();
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
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        var pluginDirectories = Directory.GetDirectories(_pluginsDirectory);
        // load from each Directory the plugin dll that also has a deps.json file
        foreach (var pluginDirectory in pluginDirectories)
        {
            var dlls = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dll in dlls)
            {
                var depsJson = Path.ChangeExtension(dll, ".deps.json");
                if (File.Exists(depsJson))
                {
                    try
                    {
                        var loadContext = new PluginLoadContext(dll);
                        var assembly = loadContext.LoadFromAssemblyPath(dll);
                        // Test assembly for pluginattribute
                        var pluginAttribute = assembly.GetCustomAttribute<PluginAttribute>();
                        if (pluginAttribute is null)
                        {
                            _logger.LogWarning("No plugin descriptor found in assembly: {PluginPath}", dll);
                            _logger.LogInformation("Unloading context for assembly: {PluginPath}", dll);
                            loadContext.Unload();
                            continue;
                        }

                        var plugin = new Plugin()
                        {
                            Id = pluginAttribute.Id,
                            Name = pluginAttribute.Name,
                            Version = pluginAttribute.Version,
                            Author = pluginAttribute.Author,
                            Description = pluginAttribute.Description,
                        };
                        //enumerate plugin interfaces in the assembly
                        Type[] allTypes;
                        try
                        {
                            allTypes = assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            _logger.LogWarning(ex, "Some types in plugin assembly could not be loaded: {PluginPath}",
                                dll);
                            allTypes = ex.Types
                                .Where(t => t != null)
                                .Cast<Type>()
                                .ToArray();
                        }

                        var initPluginType = allTypes
                            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
                        if (initPluginType != null)
                        {
                            var initPluginInstance = (IPlugin)Activator.CreateInstance(initPluginType)!;
                            plugin.InitPlugin = initPluginInstance;
                        }

                        // find other plugin types
                        // IApiProvider
                        var apiProviderTypes = allTypes
                            .Where(t => typeof(IApiProvider).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                            .ToList();
                        foreach (var apiProviderType in apiProviderTypes)
                        {
                            // scan for annotation
                            var apiProviderDesc = apiProviderType.GetCustomAttribute<ApiProviderNameAttribute>();
                            if (apiProviderDesc != null)
                            {
                                plugin.ApiProvidersByName[apiProviderDesc.Name] = new Lazy<IApiProvider>(() =>
                                    (IApiProvider)Activator.CreateInstance(apiProviderType)!);
                            }
                        }

                        if (_loadedPlugins.TryGetValue(plugin.Name, out var existingPluginEntry))
                        {
                            // A plugin with the same name is already loaded; avoid silently overwriting it.
                            var existingPlugin = existingPluginEntry.Plugin;
                            _logger.LogError(
                                "Plugin name conflict: a plugin named '{PluginName}' is already loaded (Id: {ExistingPluginId}). " +
                                "The plugin from '{PluginPath}' with Id: {NewPluginId} will not be loaded.",
                                plugin.Name,
                                existingPlugin.Id,
                                dll,
                                plugin.Id);

                            _logger.LogInformation("Unloading context for conflicting plugin assembly: {PluginPath}",
                                dll);
                            loadContext.Unload();
                            continue;
                        }

                        _loadedPlugins[plugin.Name] = (plugin, loadContext);
                        _logger.LogInformation("Loaded plugin: {PluginName} v{PluginVersion} by {PluginAuthor}",
                            plugin.Name, plugin.Version, plugin.Author);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load plugin from: {PluginPath}", dll);
                    }
                }
            }
        }

        // Also load simple plugins (single DLLs) from the root plugins directory
        foreach (var pluginPath in Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
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

                var plugin = new Plugin()
                {
                    Id = pluginAttribute.Id,
                    Name = pluginAttribute.Name,
                    Version = pluginAttribute.Version,
                    Author = pluginAttribute.Author,
                    Description = pluginAttribute.Description,
                };

                // Check for duplicate before adding
                if (_loadedPlugins.TryGetValue(plugin.Name, out var existingPluginEntry))
                {
                    var existingPlugin = existingPluginEntry.Plugin;
                    _logger.LogError(
                        "Plugin name conflict: a plugin named '{PluginName}' is already loaded (Id: {ExistingPluginId}). " +
                        "The plugin from '{PluginPath}' with Id: {NewPluginId} will not be loaded.",
                        plugin.Name,
                        existingPlugin.Id,
                        pluginPath,
                        plugin.Id);
                    loadContext.Unload();
                    continue;
                }

                // Enumerate plugin interfaces in the assembly
                Type[] allTypes;
                try
                {
                    allTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    _logger.LogWarning(rtle, "Some types in plugin assembly could not be loaded: {PluginPath}",
                        pluginPath);
                    allTypes = rtle.Types
                        .Where(t => t != null)
                        .Cast<Type>()
                        .ToArray();
                }

                var initPluginType = allTypes
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
                if (initPluginType != null)
                {
                    var initPluginInstance = (IPlugin)Activator.CreateInstance(initPluginType)!;
                    plugin.InitPlugin = initPluginInstance;
                }

                // Find IApiProvider types
                var apiProviderTypes = allTypes
                    .Where(t => typeof(IApiProvider).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                    .ToList();

                foreach (var apiProviderType in apiProviderTypes)
                {
                    var apiProviderDesc = apiProviderType.GetCustomAttribute<ApiProviderNameAttribute>();
                    if (apiProviderDesc != null)
                    {
                        plugin.ApiProvidersByName[apiProviderDesc.Name] = new Lazy<IApiProvider>(() =>
                            (IApiProvider)Activator.CreateInstance(apiProviderType)!);
                    }
                }

                _loadedPlugins[plugin.Name] = (plugin, loadContext);
                _logger.LogInformation("Loaded plugin: {PluginName} v{PluginVersion} by {PluginAuthor}",
                    plugin.Name, plugin.Version, plugin.Author);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from: {PluginPath}", pluginPath);
            }
        }
    }

    public IReadOnlyCollection<Plugin> GetLoadedPlugins() =>
        _loadedPlugins.Values.Select(p => p.Plugin).ToList();

    public async Task UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var pluginInfo))
        {
            if (pluginInfo.Plugin.InitPlugin != null)
            {
                await pluginInfo.Plugin.InitPlugin.ShutdownAsync(cancellationToken);
            }
            pluginInfo.Context.Unload();
            _loadedPlugins.Remove(pluginName);
            _logger.LogInformation("Unloaded plugin: {PluginName}", pluginName);
        }
    }
}
