using Collox.Models;
using NLog;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace Collox.Services;

public class AIService() : IAIService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Init()
    {
        Logger.Info("Initializing AI Service");
        Logger.Info("AI Service initialized successfully");
    }

    private IntelligenceConfig Config
    {
        get;
        init;
    } = JsonSettings.Configure<IntelligenceConfig>()
        .WithRecovery(RecoveryAction.RenameAndLoadDefault)
        .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
        .LoadNow();

    public void Add(IntelligentProcessor intelligentProcessor)
    {
        Logger.Debug("Adding intelligent processor: {ProcessorName} (ID: {ProcessorId})",
            intelligentProcessor.Name, intelligentProcessor.Id);

        Config.Processors ??= [];

        intelligentProcessor.ClientManager = new ChatClientManager<IntelligenceApiProvider>(
            Config.ApiProviders.FirstOrDefault(p => p.Id == intelligentProcessor.ApiProviderId));
        Config.Processors.Add(intelligentProcessor);

        Logger.Info("Added intelligent processor: {ProcessorName}", intelligentProcessor.Name);
    }

    public void Add(IntelligenceApiProvider intelligenceApiProvider)
    {
        Logger.Debug("Adding API provider: {ProviderName} (ID: {ProviderId})",
            intelligenceApiProvider.Name, intelligenceApiProvider.Id);

        Config.ApiProviders ??= [];
        if (Config.ApiProviders.Any(p => p.Id == intelligenceApiProvider.Id))
        {
            Logger.Error("API provider with ID {ProviderId} already exists", intelligenceApiProvider.Id);
            throw new InvalidOperationException("API provider with the same ID already exists.");
        }
        Config.ApiProviders.Add(intelligenceApiProvider);

        Logger.Info("Added API provider: {ProviderName}", intelligenceApiProvider.Name);
    }

    public IEnumerable<IntelligentProcessor> Get(Func<IntelligentProcessor, bool> filter)
    {
        var processors = Config.Processors.Where(filter);
        InitializeProcessors(processors);
        Logger.Debug("Retrieved {ProcessorCount} processors matching filter", processors.Count());
        return processors;
    }

    private void InitializeProcessors(IEnumerable<IntelligentProcessor> processors)
    {
        var apiProviders = Config.ApiProviders.ToDictionary(p => p.Id, p => p);
        foreach (var processor in processors)
        {
            if (apiProviders.TryGetValue(processor.ApiProviderId, out var apiProvider))
            {
                processor.ClientManager = new ChatClientManager<IntelligenceApiProvider>(apiProvider);
            }
            else
            {
                Logger.Warn("API provider {ProviderId} not found for processor {ProcessorName}",
                    processor.ApiProviderId, processor.Name);
            }
        }
    }

    // get all processors
    public IEnumerable<IntelligentProcessor> GetAll()
    {
        var processors = Config.Processors.ToList();
        InitializeProcessors(processors);
        Logger.Debug("Retrieved all {ProcessorCount} processors", processors.Count);
        return processors;
    }

    // get all API providers
    public IEnumerable<IntelligenceApiProvider> GetAllApiProviders()
    {
        var providers = Config.ApiProviders.ToList() ?? [];
        Logger.Debug("Retrieved all {ProviderCount} API providers", providers.Count);
        return providers;
    }

    public void Remove(IntelligentProcessor intelligentProcessor)
    {
        Logger.Debug("Removing intelligent processor: {ProcessorName} (ID: {ProcessorId})",
            intelligentProcessor.Name, intelligentProcessor.Id);

        Config.Processors?.Remove(intelligentProcessor);

        Logger.Info("Removed intelligent processor: {ProcessorName}", intelligentProcessor.Name);
    }

    public void Remove(IntelligenceApiProvider apiProvider)
    {
        Logger.Debug("Removing API provider: {ProviderName} (ID: {ProviderId})",
            apiProvider.Name, apiProvider.Id);

        //check that the Provider id is not used by any processor
        if (Config.Processors?.Any(p => p.ApiProviderId == apiProvider.Id) == true)
        {
            Logger.Error("Cannot remove API provider {ProviderName} - it is in use by processors", apiProvider.Name);
            throw new InvalidOperationException("Cannot remove API provider that is in use by processors.");
        }
        Config.ApiProviders?.Remove(apiProvider);

        Logger.Info("Removed API provider: {ProviderName}", apiProvider.Name);
    }

    public void Save()
    {
        Logger.Debug("Saving AI configuration");
        Config.Save();
        Logger.Info("AI configuration saved successfully");
    }

    public void Load()
    {
        Logger.Debug("Loading AI configuration");
        Config.Load();
        InitializeProcessors(Config.Processors);
        Logger.Info("AI configuration loaded successfully with {ProcessorCount} processors and {ProviderCount} providers",
            Config.Processors?.Count ?? 0, Config.ApiProviders?.Count ?? 0);
    }
}
