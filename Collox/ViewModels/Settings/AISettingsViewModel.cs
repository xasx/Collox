using System.Collections.ObjectModel;
using Collox.Models;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using Windows.ApplicationModel.Resources.Core;

namespace Collox.ViewModels;

public partial class AISettingsViewModel : ObservableObject, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<AISettingsViewModel>();
    private readonly IAIService aiService;
    private bool _initialized;
    private bool _disposed;

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
            var processors = aiService.GetAllProcessors().ToList();
            var apiProviders = aiService.GetAllApiProviders()
                .ToDictionary(p => p.Id, p => new IntelligenceApiProviderViewModel(p) { NamePresentation = "Display" });

            var availableApiProviders = new ObservableCollection<IntelligenceApiProviderViewModel>(apiProviders.Values);
            ApiProviders = availableApiProviders;

            if (processors.Count == 0)
            {
                CreateDefaultProcessor();
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

    private void CreateDefaultProcessor()
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                WeakReferenceMessenger.Default.Unregister<ProcessorDeletedMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ApiProviderDeletedMessage>(this);
            }

            _disposed = true;
        }
    }
}
