using System.Collections.ObjectModel;
using Collox.Services;
using Windows.ApplicationModel.Resources.Core;
using Collox.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;

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
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            aiService.Init();
            var processors = aiService.GetAll().ToList();

            if (processors.Count == 0)
            {
                await CreateDefaultProcessor();
            }
            else
            {
                foreach (var processor in processors)
                {
                    var vm = new IntelligentProcessorViewModel(processor) { NamePresentation = "Display" };
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
            IsEnabled = true,
            ModelId = Settings.OllamaModelId,
            Prompt = prompt,
            Provider = AIProvider.Ollama,
            Target = Target.Comment,
            FallbackId = Guid.NewGuid(),
            Name = "Synonyms",
            GetClient = aiService.GetChatClient,
        };

        aiService.Add(synonymsEnhancerProcessor);
        aiService.Save();

        var synonymsProcessorViewModel = new IntelligentProcessorViewModel(synonymsEnhancerProcessor)
        {
            NamePresentation = "Display"
        };
        Processors.Add(synonymsProcessorViewModel);
    }

    [ObservableProperty] public partial ObservableCollection<string> AvailableOllamaModelIds { get; set; } = [];

    [ObservableProperty] public partial string SelectedOllamaModelId { get; set; } = Settings.OllamaModelId;

    [ObservableProperty] public partial string OllamaAddress { get; set; } = Settings.OllamaEndpoint;

    [ObservableProperty] public partial bool IsOllamaEnabled { get; set; } = Settings.IsOllamaEnabled;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableOpenAIModelIds { get; set; } = new ObservableCollection<string>();

    [ObservableProperty] public partial string SelectedOpenAIModelId { get; set; } = Settings.OpenAIModelId;

    [ObservableProperty] public partial string OpenAIAddress { get; set; } = Settings.OpenAIEndpoint;

    [ObservableProperty] public partial string OpenAIApiKey { get; set; } = Settings.OpenAIApiKey;

    [ObservableProperty] public partial bool IsOpenAIEnabled { get; set; } = Settings.IsOpenAIEnabled;

    [ObservableProperty]
    public partial ObservableCollection<IntelligentProcessorViewModel> Processors { get; set; } = [];

    [RelayCommand]
    public async Task LoadOllamaModels()
    {
        AvailableOllamaModelIds.Clear();

        try
        {
            var models = await AIModelHelpers.GetOllamaModels().ConfigureAwait(false);

            // Batch update instead of AddRange to reduce UI notifications
            foreach (var model in models)
            {
                AvailableOllamaModelIds.Add(model);
            }
        }
        catch (Exception ex)
        {
            // Log the exception instead of silent catch
            Logger.Error(ex, "Failed to load Ollama models");
        }
    }

    [RelayCommand]
    public async Task LoadOpenAIModels()
    {
        AvailableOpenAIModelIds.Clear();

        try
        {
            var models = await AIModelHelpers.GetOpenAIModels().ConfigureAwait(false);

            // Batch update instead of AddRange to reduce UI notifications
            foreach (var model in models)
            {
                AvailableOpenAIModelIds.Add(model);
            }
        }
        catch (Exception ex)
        {
            // Log the exception instead of silent catch
            Logger.Error(ex, "Failed to load OpenAI models");
        }
    }

    [RelayCommand]
    public void AddProcessor()
    {
        var ip = new IntelligentProcessor() { Id = Guid.NewGuid(), IsEnabled = true, };

        aiService.Add(ip);
        aiService.Save();
        var vm = new IntelligentProcessorViewModel(ip);
        Processors.Add(vm);
    }

    partial void OnIsOllamaEnabledChanged(bool value) { Settings.IsOllamaEnabled = value; }

    partial void OnSelectedOllamaModelIdChanged(string value) { Settings.OllamaModelId = value; }

    partial void OnOllamaAddressChanged(string value) { Settings.OllamaEndpoint = value; }

    partial void OnSelectedOpenAIModelIdChanged(string value) { Settings.OpenAIModelId = value; }

    partial void OnOpenAIAddressChanged(string value) { Settings.OpenAIEndpoint = value; }

    partial void OnOpenAIApiKeyChanged(string value) { Settings.OpenAIApiKey = value; }

    partial void OnIsOpenAIEnabledChanged(bool value) { Settings.IsOpenAIEnabled = value; }
}

public class ProcessorDeletedMessage(IntelligentProcessorViewModel intelligentProcessorViewModel) : ValueChangedMessage<IntelligentProcessorViewModel>(
    intelligentProcessorViewModel);
