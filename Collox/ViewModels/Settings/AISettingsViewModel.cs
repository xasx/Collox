using System.ClientModel;
using System.Collections.ObjectModel;
using Collox.Services;
using Windows.ApplicationModel.Resources.Core;
using OllamaSharp;
using OpenAI.Models;

namespace Collox.ViewModels;
public partial class AISettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<string> AvailableOllamaModelIds { get; set; } = [];

    [ObservableProperty] public partial string SelectedOllamaModelId { get; set; } = Settings.OllamaModelId;

    [ObservableProperty] public partial string OllamaAddress { get; set; } = Settings.OllamaEndpoint;

    [ObservableProperty] public partial bool IsOllamaEnabled { get; set; } = Settings.IsOllamaEnabled;

    [ObservableProperty] public partial ObservableCollection<string> AvailableOpenAIModelIds { get; set; } = new ObservableCollection<string>();

    [ObservableProperty] public partial string SelectedOpenAIModelId { get; set; } = Settings.OpenAIModelId;

    [ObservableProperty] public partial string OpenAIAddress { get; set; } = Settings.OpenAIEndpoint;

    [ObservableProperty] public partial string OpenAIApiKey { get; set; } = Settings.OpenAIApiKey;

    [ObservableProperty] public partial bool IsOpenAIEnabled { get; set; } = Settings.IsOpenAIEnabled;

    [ObservableProperty] public partial ObservableCollection<MessageEnhancer> Enhancers { get; set; } = [];

    public AISettingsViewModel()
    {
        var prompt = ResourceManager.Current.MainResourceMap.GetValue("DefaultValues/SynonymsPrompt").ValueAsString;

        Enhancers.Add(new MessageEnhancer()
        {
            Id = Guid.NewGuid().ToString(),
            IsEnabled = true,
            Prompt = prompt,
            ModelId = Settings.OllamaModelId,
            Source = EnhancerSource.Ollama,
            Target = EnhancerTarget.Comment,
            ViewModelRef = this,
        });

        App.GetService<AIService>().Init();
    }
    [RelayCommand]
    public async Task LoadOllamaModels()
    {
        AvailableOllamaModelIds.Clear();

        try
        {
            using OllamaApiClient client = new OllamaApiClient(OllamaAddress);
            var models = await client.ListLocalModelsAsync();
            foreach (var model in models)
            {
                AvailableOllamaModelIds.Add(model.Name);
            }
        }
        catch (Exception ex)
        {
            // log

        }
    }

    [RelayCommand]
    public async Task LoadOpenAIModels()
    {
        AvailableOpenAIModelIds.Clear();

        try
        {
            OpenAIModelClient client = new OpenAIModelClient(
            new ApiKeyCredential(OpenAIApiKey),
            new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(OpenAIAddress) });
            var res = await client.GetModelsAsync();
            var models = res.Value;
            foreach (var model in models)
            {
                AvailableOpenAIModelIds.Add(model.Id);
            }
        }
        catch (Exception ex)
        {
        }
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
