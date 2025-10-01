using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using Serilog;

namespace Collox.Services;

public partial class ChatClientManager<T> : IDisposable, IChatClientManager where T : IChatClientFactory, INotifyPropertyChanged
{
    private static readonly ILogger Logger = Log.ForContext<ChatClientManager<T>>();

    private readonly T _clientConfig;
    private readonly ConcurrentDictionary<string, IChatClient> _clientCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1); // Binary semaphore (acts like a mutex)

    // Available models caching
    private IEnumerable<string> _cachedAvailableModels;
    private DateTime _modelsLastCached = DateTime.MinValue;
    private readonly TimeSpan _modelsCacheDuration = TimeSpan.FromMinutes(5);

    public ChatClientManager(T clientConfig)
    {
        _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));

        _clientConfig.PropertyChanged += OnConfigurationChanged;
        Logger.Debug("ChatClientManager initialized for {ClientConfig}", clientConfig);
    }

    public async Task<IChatClient> GetChatClientAsync(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        // Try to get existing client first (no lock needed)
        if (_clientCache.TryGetValue(modelId, out var cachedClient))
        {
            Logger.Debug("Retrieved cached client for model {ModelId}", modelId);
            return cachedClient;
        }

        // Only lock when we need to create a new client
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check pattern - another thread might have created it while we waited
            var client = _clientCache.GetOrAdd(modelId, id =>
            {
                Logger.Debug("Creating new client for model {ModelId}", id);
                var client = _clientConfig.CreateClient(id);

                client = new ChatClientBuilder(client)
                    .UseFunctionInvocation()
                    .Build();
                return client;
            });

            Logger.Information("Chat client ready for model {ModelId}", modelId);
            return client;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    // Keep synchronous version for compatibility
    public IChatClient GetChatClient(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        _cacheLock.Wait();
        try
        {
            if (_clientCache.TryGetValue(modelId, out var cachedClient))
            {
                Logger.Debug("Retrieved cached client for model {ModelId}", modelId);
                return cachedClient;
            }

            Logger.Debug("Creating new client for model {ModelId}", modelId);
            var newClient = _clientConfig.CreateClient(modelId);
            _clientCache[modelId] = newClient;
            Logger.Information("Chat client created and cached for model {ModelId}", modelId);
            return newClient;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public Task<IEnumerable<string>> AvailableModels => GetAvailableModelsAsync();

    private async Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        // Keep the lock here because _cachedAvailableModels is NOT thread-safe
        await _cacheLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            if (_cachedAvailableModels == null || now - _modelsLastCached > _modelsCacheDuration)
            {
                Logger.Debug("Refreshing available models cache");
                _cachedAvailableModels = await _clientConfig.AvailableModels;
                _modelsLastCached = now;
                Logger.Information("Available models cache refreshed. Found {ModelCount} models", _cachedAvailableModels?.Count() ?? 0);
            }
            return _cachedAvailableModels;
        }
        catch(Exception ex)
        {
            Logger.Error(ex, "Error occurred while fetching available models");
            throw;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private void OnConfigurationChanged(object sender, PropertyChangedEventArgs e)
    {
        Logger.Information("Configuration changed for property {PropertyName}, clearing client cache", e.PropertyName);

        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheLock.WaitAsync();
                try
                {
                    // Clear with proper disposal
                    foreach (var client in _clientCache.Values)
                    {
                        if (client is IDisposable disposableClient)
                        {
                            disposableClient.Dispose();
                        }
                    }
                    _clientCache.Clear(); // ConcurrentDictionary.Clear() is thread-safe
                    _cachedAvailableModels = null;
                    _modelsLastCached = DateTime.MinValue;

                    Logger.Debug("Client cache cleared successfully");
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while clearing client cache");
            }
        });
    }

    public void Dispose()
    {
        Logger.Debug("Disposing ChatClientManager");

        if (_clientConfig is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnConfigurationChanged;
        }

        _cacheLock.Wait();
        try
        {
            // Dispose cached clients before clearing
            foreach (var client in _clientCache.Values)
            {
                if (client is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }
            }
            _clientCache.Clear();
            _cachedAvailableModels = null;

            Logger.Debug("ChatClientManager disposed successfully");
        }
        finally
        {
            _cacheLock.Release();
            _cacheLock.Dispose();
        }
    }
}
