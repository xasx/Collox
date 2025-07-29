using OllamaSharp;
using OpenAI;
using System.ClientModel;

namespace Collox.Services;

public class AIApis : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
    private volatile bool _initialized;
    private volatile bool _disposed;
    private OllamaApiClient? _ollama;
    private OpenAIClient? _openAI;
    
    // Track last known configuration to detect changes
    private string? _lastOllamaEndpoint;
    private string? _lastOpenAIEndpoint;
    private string? _lastOpenAIApiKey;

    public OllamaApiClient Ollama 
    { 
        get 
        {
            ThrowIfDisposed();
            EnsureInitialized();
            CheckAndUpdateOllamaClient();
            return _ollama!;
        }
    }

    public OpenAIClient OpenAI 
    { 
        get 
        {
            ThrowIfDisposed();
            EnsureInitialized();
            CheckAndUpdateOpenAIClient();
            return _openAI!;
        }
    }

    public void Init()
    {
        ThrowIfDisposed();
        
        if (_initialized)
            return; // Prevent duplicate initialization

        _lock.EnterWriteLock();
        try
        {
            if (_initialized)
                return; // Double-check pattern

            CreateClients();
            _initialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void ForceReconfigure()
    {
        ThrowIfDisposed();
        
        _lock.EnterWriteLock();
        try
        {
            CreateClients();
            _initialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool IsInitialized => _initialized;

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("AIApis must be initialized before accessing clients. Call Init() first.");
    }

    private void CheckAndUpdateOllamaClient()
    {
        var currentEndpoint = Settings.OllamaEndpoint;
        
        // Use read lock for checking configuration
        _lock.EnterReadLock();
        try
        {
            if (_lastOllamaEndpoint == currentEndpoint)
                return; // No change needed
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Configuration changed, need write lock to update
        _lock.EnterWriteLock();
        try
        {
            // Double-check pattern for configuration change
            if (_lastOllamaEndpoint != currentEndpoint)
            {
                // Only dispose Ollama client if it implements IDisposable
                if (_ollama is IDisposable disposableOllama)
                    disposableOllama.Dispose();
                
                _ollama = new OllamaApiClient(currentEndpoint);
                _lastOllamaEndpoint = currentEndpoint;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void CheckAndUpdateOpenAIClient()
    {
        var currentEndpoint = Settings.OpenAIEndpoint;
        var currentApiKey = Settings.OpenAIApiKey;
        
        // Use read lock for checking configuration
        _lock.EnterReadLock();
        try
        {
            if (_lastOpenAIEndpoint == currentEndpoint && _lastOpenAIApiKey == currentApiKey)
                return; // No change needed
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Configuration changed, need write lock to update
        _lock.EnterWriteLock();
        try
        {
            // Double-check pattern for configuration change
            if (_lastOpenAIEndpoint != currentEndpoint || _lastOpenAIApiKey != currentApiKey)
            {
                // OpenAI client is not disposable, just replace reference
                _openAI = new OpenAIClient(new ApiKeyCredential(currentApiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(currentEndpoint) });
                _lastOpenAIEndpoint = currentEndpoint;
                _lastOpenAIApiKey = currentApiKey;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void CreateClients()
    {
        var ollamaEndpoint = Settings.OllamaEndpoint;
        var openAIEndpoint = Settings.OpenAIEndpoint;
        var openAIApiKey = Settings.OpenAIApiKey;

        // Only dispose Ollama client if it implements IDisposable
        if (_ollama is IDisposable disposableOllama)
            disposableOllama.Dispose();

        // OpenAI client is not disposable, just replace reference
        _ollama = new OllamaApiClient(ollamaEndpoint);
        _openAI = new OpenAIClient(new ApiKeyCredential(openAIApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(openAIEndpoint) });

        _lastOllamaEndpoint = ollamaEndpoint;
        _lastOpenAIEndpoint = openAIEndpoint;
        _lastOpenAIApiKey = openAIApiKey;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AIApis));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
                return;

            // Only dispose Ollama client if it implements IDisposable
            if (_ollama is IDisposable disposableOllama)
                disposableOllama.Dispose();

            // OpenAI client is not disposable, just clear reference
            _openAI = null;
            _disposed = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _lock.Dispose();
    }
}
