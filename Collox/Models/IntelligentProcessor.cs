using Collox.Services;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace Collox.Models;

public partial class IntelligentProcessor : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<IntelligentProcessor>();
    private bool _disposed;

    public Guid Id { get; init; }

    public string Name { get; set; }

    public Guid ApiProviderId { get; set; }

    [JsonIgnore] public IChatClientManager ClientManager { get; set; }

    public string Prompt { get; set; }

    public string SystemPrompt { get; set; }

    public string ModelId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Target Target { get; set; }

    public Guid FallbackId { get; set; }

    public delegate Task<string> ProcessMessage(MessageProcessingContext context, IntelligentProcessor processor,
        IChatClient client, CancellationToken cancellationToken);

    public delegate void ErrorHandler(Exception exception);

    [JsonIgnore] public ProcessMessage Process { get; set; }

    [JsonIgnore] public ErrorHandler OnError { get; set; }

    public async Task Work(MessageProcessingContext context, CancellationToken cancellationToken = default)
    {
        Logger.Information(
            "Starting work for processor '{ProcessorName}' (ID: {ProcessorId}) using {ClientManager} model '{ModelId}'",
            Name, Id, ClientManager, ModelId);

        if (ClientManager is null)
        {
            Logger.Error("ClientManager is null for processor '{ProcessorName}' (ID: {ProcessorId}). Skipping.", Name, Id);
            OnError?.Invoke(new InvalidOperationException($"No API provider configured for processor '{Name}'."));
            return;
        }

        if (Process is null)
        {
            Logger.Error("Process delegate is null for processor '{ProcessorName}' (ID: {ProcessorId}). Skipping.", Name, Id);
            OnError?.Invoke(new InvalidOperationException($"No processing function assigned for processor '{Name}'."));
            return;
        }

        try
        {
            var client = await ClientManager.GetChatClientAsync(ModelId);
            var result = await Process(context, this, client, cancellationToken);
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

    public void Dispose()
    {
        if (_disposed)
            return;

        Logger.Debug("Disposing IntelligentProcessor: {ProcessorName} (ID: {ProcessorId})", Name, Id);

        if (ClientManager is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
        Logger.Information("IntelligentProcessor disposed: {ProcessorName}", Name);
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
