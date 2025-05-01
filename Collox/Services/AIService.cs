using Collox.Models;
using Microsoft.Extensions.AI;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace Collox.Services;
public class AIService(AIApis apis)
{
    public void Init()
    {
        apis.Init();

    }

    private IntelligenceConfig Config { get; init; } = JsonSettings.Configure<IntelligenceConfig>()
        .WithRecovery(RecoveryAction.RenameAndLoadDefault)
            .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
        
            .LoadNow();

    public IChatClient GetChatClient(AIProvider apiType, string modelId)
    {
        switch (apiType)
        {
            case AIProvider.Ollama:
                apis.Ollama.SelectedModel = modelId;
                return apis.Ollama;
            case AIProvider.OpenAI:
                return apis.OpenAI.GetChatClient(modelId).AsIChatClient();
            default:
                throw new NotSupportedException($"API type {apiType} is not supported.");
        }
    }

    public void Add(IntelligentProcessor intelligentProcessor)
    {
        if (Config.Processors == null)
        {
            Config.Processors = new List<IntelligentProcessor>();
        }

        intelligentProcessor.GetClient = GetChatClient;

        Config.Processors.Add(intelligentProcessor);
    }

    public  IEnumerable<IntelligentProcessor> Get(Func<IntelligentProcessor, bool> filter)
    {
        var processors = Config.Processors.Where(filter).ToList();
        foreach (var processor in processors)
        {
            processor.GetClient = GetChatClient;
            yield return processor;

        }
    }

    


    

    public void Remove(IntelligentProcessor intelligentProcessor)
    {
        Config.Processors?.Remove(intelligentProcessor);
    }
    public void Save()
    {
        Config.Save();
    }

    public void Load()
    {
        Config.Load();
        Config.Processors.ForEach(p => p.GetClient = GetChatClient);
    }

}
