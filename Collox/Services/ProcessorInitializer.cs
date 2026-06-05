using Collox.Models;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Collox.Services;

public static class ProcessorInitializer
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ProcessorInitializer));

    public static void Initialize(
        IEnumerable<IntelligentProcessor> processors,
        IReadOnlyDictionary<Guid, IntelligenceApiProvider> apiProviders)
    {
        foreach (var processor in processors)
        {
            if (!apiProviders.TryGetValue(processor.ApiProviderId, out var apiProvider))
            {
                Logger.Warning("API provider {ProviderId} not found for processor {ProcessorName}",
                    processor.ApiProviderId, processor.Name);
                continue;
            }

            if (processor.ClientManager is not null) continue;

            processor.ClientManager = new ChatClientManager<IntelligenceApiProvider>(apiProvider);
        }
    }
}
