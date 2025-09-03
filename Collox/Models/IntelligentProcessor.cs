using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace Collox.Models;

public partial class IntelligentProcessor
{
    private static readonly ILogger Logger = Log.ForContext<IntelligentProcessor>();

    public Guid Id { get; init; }

    public string Name { get; set; }

    public Guid ApiProviderId { get; set; }

    [JsonIgnore]
    public IChatClientManager ClientManager { get; set; }

    public string Prompt { get; set; }

    public string SystemPrompt { get; set; }

    public string ModelId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Target Target { get; set; }

    public Guid FallbackId { get; set; }

    public delegate Task<string> ProcessMessage(IChatClient chatClient);

    public delegate void ErrorHandler(Exception exception);

    [JsonIgnore] public ProcessMessage Process { get; set; }

    [JsonIgnore] public ErrorHandler OnError { get; set; }

    public async Task Work()
    {
        Logger.Information("Starting work for processor '{ProcessorName}' (ID: {ProcessorId}) using {ClientManager} model '{ModelId}'",
            Name, Id, ClientManager, ModelId);

        var client = ClientManager?.GetChatClient(ModelId);

        try
        {
            var result = await Process(client);
            Logger.Information("Successfully completed processing for '{ProcessorName}'. Result length: {ResultLength}",
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
