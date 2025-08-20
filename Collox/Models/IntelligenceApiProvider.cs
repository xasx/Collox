using System.ClientModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using OllamaSharp;
using OpenAI;
using OpenAI.Models;

namespace Collox.Models;

public partial class IntelligenceApiProvider : IChatClientFactory, INotifyPropertyChanged
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event PropertyChangedEventHandler PropertyChanged;

    public string ApiKey
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Logger.Info("API key changed for {ProviderId}", Id);
                OnPropertyChanged();
            }
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public AIProvider ApiType
    {
        get;
        set
        {
            if (field != value)
            {
                Logger.Info("API provider type changed from {OldType} to {NewType} for {ProviderId}", field, value, Id);
                field = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    public Task<IEnumerable<string>> AvailableModels => ApiType switch
    {
        AIProvider.Ollama => GetOllamaModels(),
        AIProvider.OpenAI => GetOpenAIModels(),
        _ => throw new NotSupportedException($"API provider '{ApiType}' is not supported.")
    };

    public string Endpoint
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Logger.Info("Endpoint changed to {Endpoint} for {ProviderId}", field, Id);
                OnPropertyChanged();
            }
        }
    }

    public Guid Id { get; init; }

    public string Name { get; set; }

    public IChatClient CreateClient(string modelId)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        Logger.Debug("Creating {ApiType} client for model {ModelId}", ApiType, modelId);

        return ApiType switch
        {
            AIProvider.Ollama => CreateOllamaClient(modelId),
            AIProvider.OpenAI => CreateOpenAIClient(modelId),
            _ => throw new NotSupportedException($"API provider '{ApiType}' is not supported.")
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    private IChatClient CreateOllamaClient(string modelId)
    {
        ArgumentException.ThrowIfNullOrEmpty(Endpoint);
        return new OllamaApiClient(Endpoint, modelId);
    }

    private IChatClient CreateOpenAIClient(string modelId)
    {
        ArgumentException.ThrowIfNullOrEmpty(ApiKey);
        ArgumentException.ThrowIfNullOrEmpty(Endpoint);

        var credentials = new ApiKeyCredential(ApiKey);
        var options = new OpenAIClientOptions { Endpoint = new Uri(Endpoint) };
        var openAIClient = new OpenAIClient(credentials, options);

        return openAIClient.GetChatClient(modelId).AsIChatClient();
    }

    private async Task<IEnumerable<string>> GetOllamaModels(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.Debug("Fetching Ollama models from {Endpoint}", Endpoint);

            using var client = new OllamaApiClient(Endpoint);
            var models = await client.ListLocalModelsAsync(cancellationToken).ConfigureAwait(false);
            var result = models.Select(m => m.Name).ToList();

            if (Logger.IsInfoEnabled)
                Logger.Info("Retrieved {ModelCount} Ollama models", result.Count);

            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "Failed to connect to Ollama at {Endpoint}", Endpoint);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama at {Endpoint}. Please check if Ollama is running.",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.Warn(ex, "Timeout connecting to Ollama at {Endpoint}", Endpoint);
            throw new TimeoutException($"Timeout connecting to Ollama at {Endpoint}", ex);
        }
    }

    private async Task<IEnumerable<string>> GetOpenAIModels(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Logger.Error("OpenAI API key not configured for provider {ProviderId}", Id);
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            Logger.Debug("Fetching OpenAI models from {Endpoint}", Endpoint);

            var client = new OpenAIModelClient(
                new ApiKeyCredential(ApiKey),
                new OpenAIClientOptions() { Endpoint = new Uri(Endpoint) });

            var res = await client.GetModelsAsync(cancellationToken).ConfigureAwait(false);
            var result = res.Value?.Select(m => m.Id).ToList() ?? [];

            if (Logger.IsInfoEnabled)
                Logger.Info("Retrieved {ModelCount} OpenAI models", result.Count());

            return result;
        }
        catch (ClientResultException ex) when (ex.Status == 401)
        {
            Logger.Error(ex, "Invalid OpenAI API key for {Endpoint}", Endpoint);
            throw new UnauthorizedAccessException("Invalid OpenAI API key", ex);
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            Logger.Warn(ex, "OpenAI rate limit exceeded for {Endpoint}", Endpoint);
            throw new InvalidOperationException("OpenAI API rate limit exceeded", ex);
        }
    }
}
