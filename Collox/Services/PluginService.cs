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

public class PluginService : IPluginService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PluginService> _logger;
    private readonly Dictionary<string, (Plugin Plugin, PluginLoadContext Context)> _loadedPlugins = new();
    private readonly string _pluginsDirectory;
    private bool _disposed;

    public PluginService(IServiceProvider services, ILogger<PluginService> logger)
    {
        _services = services;
        _logger = logger;
        _pluginsDirectory = Path.Combine(Settings.BaseFolder, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    /// <summary>
    /// Phase 1: Discover and load plugin assemblies. Does not call InitializeAsync.
    /// </summary>
    public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading plugins from: {PluginsDirectory}", _pluginsDirectory);
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        // Load plugins from subdirectories (those with deps.json)
        foreach (var pluginDirectory in Directory.GetDirectories(_pluginsDirectory))
        {
            var dlls = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dll in dlls)
            {
                var depsJson = Path.ChangeExtension(dll, ".deps.json");
                if (File.Exists(depsJson))
                {
                    TryLoadPlugin(dll);
                }
            }
        }

        // Also load simple plugins (single DLLs) from the root plugins directory
        foreach (var pluginPath in Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            TryLoadPlugin(pluginPath);
        }
    }

    /// <summary>
    /// Phase 2: Initialize all loaded plugins that have an IPlugin implementation.
    /// </summary>
    public async Task InitializePluginsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (name, (plugin, _)) in _loadedPlugins)
        {
            if (plugin.InitPlugin != null)
            {
                try
                {
                    await plugin.InitPlugin.InitializeAsync(_services, cancellationToken);
                    _logger.LogInformation("Initialized plugin: {PluginName}", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize plugin: {PluginName}", name);
                }
            }
        }
    }

    private void TryLoadPlugin(string dllPath)
    {
        PluginLoadContext loadContext = null;
        try
        {
            loadContext = new PluginLoadContext(dllPath);
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            var pluginAttribute = assembly.GetCustomAttribute<PluginAttribute>();
            if (pluginAttribute is null)
            {
                _logger.LogWarning("No plugin descriptor found in assembly: {PluginPath}", dllPath);
                loadContext.Unload();
                return;
            }

            var plugin = new Plugin()
            {
                Id = pluginAttribute.Id,
                Name = pluginAttribute.Name,
                Version = pluginAttribute.Version,
                Author = pluginAttribute.Author,
                Description = pluginAttribute.Description,
            };

            // Check for duplicate before doing any further work
            if (_loadedPlugins.TryGetValue(plugin.Name, out var existingPluginEntry))
            {
                _logger.LogError(
                    "Plugin name conflict: a plugin named '{PluginName}' is already loaded (Id: {ExistingPluginId}). " +
                    "The plugin from '{PluginPath}' with Id: {NewPluginId} will not be loaded.",
                    plugin.Name,
                    existingPluginEntry.Plugin.Id,
                    dllPath,
                    plugin.Id);
                loadContext.Unload();
                return;
            }

            // Enumerate types in the assembly
            Type[] allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning(ex, "Some types in plugin assembly could not be loaded: {PluginPath}", dllPath);
                allTypes = ex.Types
                    .Where(t => t != null)
                    .Cast<Type>()
                    .ToArray();
            }

            // Find optional IPlugin implementation (for plugins that need initialization)
            var initPluginType = allTypes
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t)
                                     && !t.IsAbstract && t.IsClass);
            if (initPluginType != null)
            {
                var initPluginInstance = (IPlugin)Activator.CreateInstance(initPluginType)!;
                plugin.InitPlugin = initPluginInstance;
            }

            // Find IApiProvider types (independent of IPlugin)
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
            _logger.LogError(ex, "Failed to load plugin from: {PluginPath}", dllPath);
            // Clean up the load context if we created one but failed
            loadContext?.Unload();
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

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var (name, (plugin, context)) in _loadedPlugins)
        {
            try
            {
                plugin.InitPlugin?.ShutdownAsync().GetAwaiter().GetResult();
                context.Unload();
                _logger.LogInformation("Shut down plugin: {PluginName}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down plugin: {PluginName}", name);
            }
        }
        _loadedPlugins.Clear();
        _disposed = true;
    }
}
