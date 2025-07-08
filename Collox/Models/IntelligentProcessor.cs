using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;

namespace Collox.Models;

public partial class IntelligentProcessor
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Guid Id { get; set; }

    public string Name { get; set; }

    public bool IsEnabled { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public AIProvider Provider { get; set; }

    public string Prompt { get; set; }

    public string ModelId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Target Target { get; set; }

    public Guid FallbackId { get; set; }

    public delegate IChatClient ClientRetriever(AIProvider provider, string modelId);

    public delegate Task<string> ProcessMessage(IChatClient chatClient);

    public delegate void ErrorHandler(Exception exception);

    [JsonIgnore] public ClientRetriever GetClient { get; set; }

    [JsonIgnore] public ProcessMessage Process { get; set; }

    [JsonIgnore] public ErrorHandler OnError { get; set; }

    public async Task Work()
    {
        Logger.Info("Starting work for processor '{ProcessorName}' (ID: {ProcessorId}) using {Provider} model '{ModelId}'", 
            Name, Id, Provider, ModelId);

        var client = GetClient(Provider, ModelId);

        try
        {
            var result = await Process(client);
            Logger.Info("Successfully completed processing for '{ProcessorName}'. Result length: {ResultLength}", 
                Name, result?.Length ?? 0);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred during processing for '{ProcessorName}' (ID: {ProcessorId})", 
                Name, Id);
            OnError?.Invoke(ex);
        }
    }
}

public enum Target
{
    Comment,
    Task,
    Message,
    Chat
}

public enum AIProvider
{
    Ollama,
    OpenAI
}
