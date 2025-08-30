using System.Collections.ObjectModel;
using Collox.Models;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.ApplicationModel.Resources.Core;

namespace Collox.ViewModels;

public partial class AISettingsViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly IAIService aiService;
    private bool _initialized;

    public AISettingsViewModel(IAIService aIService)
    {
        aiService = aIService;
        // Don't initialize immediately - wait for first access

        WeakReferenceMessenger.Default
            .Register<ProcessorDeletedMessage>(
                this,
                (r, m) =>
                {
                    Processors.Remove(m.Value);
                    aiService.Remove(m.Value.Model);
                    aiService.Save();
                });
        WeakReferenceMessenger.Default
            .Register<ApiProviderDeletedMessage>(
                this,
                (r, m) =>
                {
                    ApiProviders.Remove(m.Value);
                    aiService.Remove(m.Value.Model);
                    aiService.Save();
                });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            var processors = aiService.GetAll().ToList();
            var apiProviders = aiService.GetAllApiProviders()
                .ToDictionary(p => p.Id, p => new IntelligenceApiProviderViewModel(p) { NamePresentation = "Display" });

            var availableApiProviders = new ObservableCollection<IntelligenceApiProviderViewModel>(apiProviders.Values);
            ApiProviders = availableApiProviders;

            if (processors.Count == 0)
            {
                await CreateDefaultProcessor();
            }
            else
            {
                foreach (var processor in processors)
                {
                    var vm = new IntelligentProcessorViewModel(processor) { NamePresentation = "Display" };
                    var prov = processor.ApiProviderId;

                    if (apiProviders.TryGetValue(prov, out var apiProvider))
                    {
                        vm.Provider = apiProvider;
                    }

                    vm.Providers = availableApiProviders;
                    vm.AvailableModelIds.AddRange(await vm.Model.ClientManager.AvailableModels);

                    Processors.Add(vm);
                }
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize AI settings");
        }
    }

    private async Task CreateDefaultProcessor()
    {
        var prompt = ResourceManager.Current.MainResourceMap.GetValue("DefaultValues/SynonymsPrompt").ValueAsString;
        var synonymsEnhancerProcessor = new IntelligentProcessor()
        {
            Id = Guid.NewGuid(),
            ModelId = " default",
            Prompt = prompt,
            Target = Target.Comment,
            FallbackId = Guid.NewGuid(),
            Name = "Synonyms"
        };

        aiService.Add(synonymsEnhancerProcessor);
        aiService.Save();

        var synonymsProcessorViewModel = new IntelligentProcessorViewModel(synonymsEnhancerProcessor)
        {
            NamePresentation = "Display"
        };
        Processors.Add(synonymsProcessorViewModel);
    }

    [ObservableProperty]
    public partial ObservableCollection<IntelligentProcessorViewModel> Processors { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<IntelligenceApiProviderViewModel> ApiProviders { get; set; } = [];


    [RelayCommand]
    public void AddProcessor()
    {
        var ip = new IntelligentProcessor() { Id = Guid.NewGuid(), };

        aiService.Add(ip);
        aiService.Save();
        var vm = new IntelligentProcessorViewModel(ip);
        Processors.Add(vm);
    }

    [RelayCommand]
    public void AddApiProvider()
    {
        var provider = new IntelligenceApiProvider() { Id = Guid.NewGuid() };
        aiService.Add(provider);
        aiService.Save();
        var vm = new IntelligenceApiProviderViewModel(provider);
        ApiProviders.Add(vm);
    }
}

public class ProcessorDeletedMessage(IntelligentProcessorViewModel intelligentProcessorViewModel) : ValueChangedMessage<IntelligentProcessorViewModel>(
    intelligentProcessorViewModel);


public class ApiProviderDeletedMessage(IntelligenceApiProviderViewModel intelligenceApiProviderViewModel) : ValueChangedMessage<IntelligenceApiProviderViewModel>(
    intelligenceApiProviderViewModel);
