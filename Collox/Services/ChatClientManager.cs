using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Collox.Services;
public partial class ChatClientManager<T> : IDisposable, IChatClientManager where T : IChatClientFactory, INotifyPropertyChanged
{
    private readonly T _clientConfig;
    private readonly Dictionary<string, IChatClient> _clientCache = [];
    private readonly SemaphoreSlim _cacheLock = new(1, 1); // Binary semaphore (acts like a mutex)

    // Available models caching
    private IEnumerable<string> _cachedAvailableModels;
    private DateTime _modelsLastCached = DateTime.MinValue;
    private readonly TimeSpan _modelsCacheDuration = TimeSpan.FromMinutes(5);

    public ChatClientManager(T clientConfig)
    {
        _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));

        _clientConfig.PropertyChanged += OnConfigurationChanged;
    }

    public async Task<IChatClient> GetChatClientAsync(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        await _cacheLock.WaitAsync();
        try
        {
            if (_clientCache.TryGetValue(modelId, out var cachedClient))
            {
                return cachedClient;
            }

            var newClient = _clientConfig.CreateClient(modelId);
            _clientCache[modelId] = newClient;
            return newClient;
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
                return cachedClient;
            }

            var newClient = _clientConfig.CreateClient(modelId);
            _clientCache[modelId] = newClient;
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
        await _cacheLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            if (_cachedAvailableModels == null || (now - _modelsLastCached) > _modelsCacheDuration)
            {
                _cachedAvailableModels = await _clientConfig.AvailableModels;
                _modelsLastCached = now;
            }
            return _cachedAvailableModels;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async void OnConfigurationChanged(object sender, PropertyChangedEventArgs e)
    {
        await _cacheLock.WaitAsync();
        try
        {
            _clientCache.Clear();
            _cachedAvailableModels = null;
            _modelsLastCached = DateTime.MinValue;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public void Dispose()
    {
        if (_clientConfig is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnConfigurationChanged;
        }

        _cacheLock.Wait();
        try
        {
            _clientCache.Clear();
            _cachedAvailableModels = null;
        }
        finally
        {
            _cacheLock.Release();
            _cacheLock.Dispose();
        }
    }
}
