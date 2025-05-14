using System.Collections.ObjectModel;
using Collox.Services;
using Windows.ApplicationModel.Resources.Core;
using Collox.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class AISettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<string> AvailableOllamaModelIds { get; set; } = [];

    [ObservableProperty] public partial string SelectedOllamaModelId { get; set; } = Settings.OllamaModelId;

    [ObservableProperty] public partial string OllamaAddress { get; set; } = Settings.OllamaEndpoint;

    [ObservableProperty] public partial bool IsOllamaEnabled { get; set; } = Settings.IsOllamaEnabled;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableOpenAIModelIds { get; set; } =
        new ObservableCollection<string>();

    [ObservableProperty] public partial string SelectedOpenAIModelId { get; set; } = Settings.OpenAIModelId;

    [ObservableProperty] public partial string OpenAIAddress { get; set; } = Settings.OpenAIEndpoint;

    [ObservableProperty] public partial string OpenAIApiKey { get; set; } = Settings.OpenAIApiKey;

    [ObservableProperty] public partial bool IsOpenAIEnabled { get; set; } = Settings.IsOpenAIEnabled;

    [ObservableProperty]
    public partial ObservableCollection<IntelligentProcessorViewModel> Enhancers { get; set; } = [];

    private readonly AIService aiService = App.GetService<AIService>();

    public AISettingsViewModel()
    {
        var prompt = ResourceManager.Current.MainResourceMap.GetValue("DefaultValues/SynonymsPrompt").ValueAsString;


        aiService.Init();
        var processors = aiService.GetAll().ToList();
        if (!processors.Any())
        {
            var SynonymsEnhancerProcessor = new IntelligentProcessor()
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
            aiService.Add(SynonymsEnhancerProcessor);
            aiService.Save();


            var synonymsProcessorViewModel = new IntelligentProcessorViewModel(SynonymsEnhancerProcessor)
            {
                NamePresentation = "Display"
            };
            Enhancers.Add(synonymsProcessorViewModel);
        }
        else
        {
            foreach (var processor in processors)
            {
                var vm = new IntelligentProcessorViewModel(processor)
                {
                    NamePresentation = "Display"
                };
                Enhancers.Add(vm);
            }
        }

        WeakReferenceMessenger.Default.Register<ProcessorDeletedMessage>(this, (r, m) =>
        {
            Enhancers.Remove(m.Value);
            aiService.Remove(m.Value.Model);
            aiService.Save();
        });
    }

    [RelayCommand]
    public async Task LoadOllamaModels()
    {
        AvailableOllamaModelIds.Clear();

        try
        {
            AvailableOllamaModelIds.AddRange(await AIModelHelpers.GetOllamaModels());
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    public async Task LoadOpenAIModels()
    {
        AvailableOpenAIModelIds.Clear();

        try
        {
            AvailableOpenAIModelIds.AddRange(await AIModelHelpers.GetOpenAIModels());
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    public void AddProcessor()
    {
        var ip = new IntelligentProcessor()
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
        };

        aiService.Add(ip);
        aiService.Save();
        var vm = new IntelligentProcessorViewModel(ip);
        Enhancers.Add(vm);
    }

    partial void OnIsOllamaEnabledChanged(bool value)
    {
        Settings.IsOllamaEnabled = value;
    }


    partial void OnSelectedOllamaModelIdChanged(string value)
    {
        Settings.OllamaModelId = value;
    }

    partial void OnOllamaAddressChanged(string value)
    {
        Settings.OllamaEndpoint = value;
    }


    partial void OnSelectedOpenAIModelIdChanged(string value)
    {
        Settings.OpenAIModelId = value;
    }

    partial void OnOpenAIAddressChanged(string value)
    {
        Settings.OpenAIEndpoint = value;
    }

    partial void OnOpenAIApiKeyChanged(string value)
    {
        Settings.OpenAIApiKey = value;
    }

    partial void OnIsOpenAIEnabledChanged(bool value)
    {
        Settings.IsOpenAIEnabled = value;
    }
}

public class ProcessorDeletedMessage(IntelligentProcessorViewModel intelligentProcessorViewModel)
    : ValueChangedMessage<IntelligentProcessorViewModel>(intelligentProcessorViewModel);
