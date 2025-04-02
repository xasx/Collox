using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OllamaSharp;
using OpenAI.Models;

namespace Collox.ViewModels;
public partial class AISettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<string> AvailableOllamaModelIds { get; set; } = new ObservableCollection<string>();

    [ObservableProperty] public partial string SelectedOllamaModelId { get; set; } = AppHelper.Settings.OllamaModelId;

    [ObservableProperty] public partial string OllamaAddress { get; set; } = AppHelper.Settings.OllamaEndpoint;

    [ObservableProperty] public partial bool IsOllamaEnabled { get; set; } = AppHelper.Settings.OllamaEnabled;

    [ObservableProperty] public partial ObservableCollection<string> AvailableOpenAIModelIds { get; set; } = new ObservableCollection<string>();

    [ObservableProperty] public partial string SelectedOpenAIModelId { get; set; } = AppHelper.Settings.OpenAIModelId;

    [ObservableProperty] public partial string OpenAIAddress { get; set; } = AppHelper.Settings.OpenAIEndpoint;

    [ObservableProperty] public partial string OpenAIApiKey { get; set; } = AppHelper.Settings.OpenAIApiKey;

    [ObservableProperty] public partial bool IsOpenAIEnabled { get; set; } = AppHelper.Settings.OpenAIEnabled;


    [RelayCommand]
    public async void LoadOllamaModels()
    {
        AvailableOllamaModelIds.Clear();

        OllamaApiClient client = new OllamaApiClient(OllamaAddress);
        var models = await client.ListLocalModelsAsync();
        foreach (var model in models)
        {
            AvailableOllamaModelIds.Add(model.Name);
        }
    }

    [RelayCommand]
    public async void LoadOpenAIModels()
    {
        AvailableOpenAIModelIds.Clear();
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

    partial void OnIsOllamaEnabledChanged(bool value)
    {
        AppHelper.Settings.OllamaEnabled = value;
    }


    partial void OnSelectedOllamaModelIdChanged(string value)
    {
        AppHelper.Settings.OllamaModelId = value;
    }

    partial void OnOllamaAddressChanged(string value)
    {
        AppHelper.Settings.OllamaEndpoint = value;
    }


    partial void OnSelectedOpenAIModelIdChanged(string value)
    {
        AppHelper.Settings.OpenAIModelId = value;
    }

    partial void OnOpenAIAddressChanged(string value)
    {
        AppHelper.Settings.OpenAIEndpoint = value;
    }

    partial void OnOpenAIApiKeyChanged(string value)
    {
        AppHelper.Settings.OpenAIApiKey = value;
    }

    partial void OnIsOpenAIEnabledChanged(bool value)
    {
        AppHelper.Settings.OpenAIEnabled = value;
    }

     
}
