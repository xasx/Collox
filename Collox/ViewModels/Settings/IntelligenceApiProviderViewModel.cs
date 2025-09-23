using Collox.Models;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class IntelligenceApiProviderViewModel : ObservableObject
{
    [ObservableProperty] public partial Guid Id { get; set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty] public partial string ApiKey { get; set; } = string.Empty;

    [ObservableProperty] public partial SourceProvider ApiType { get; set; }

    [ObservableProperty] public partial string NamePresentation { get; set; } = "Edit";

    public IntelligenceApiProvider Model { get; init; }

    public IntelligenceApiProviderViewModel(IntelligenceApiProvider model)
    {
        Model = model;
        Id = model.Id;
        Name = model.Name;
        Endpoint = model.Endpoint;
        ApiKey = model.ApiKey;
        ApiType = model.ApiType switch
        {
            AIProvider.OpenAI => SourceProvider.OpenAI,
            AIProvider.Ollama => SourceProvider.Ollama,
            _ => throw new NotSupportedException($"API provider '{model.ApiType}' is not supported.")
        };

    }

    partial void OnApiKeyChanged(string oldValue, string newValue)
    {
        if (oldValue != newValue)
        {
            Model.ApiKey = newValue;
            SaveModel();
        }
    }

    partial void OnEndpointChanged(string oldValue, string newValue)
    {
        if (oldValue != newValue)
        {
            Model.Endpoint = newValue;
            SaveModel();
        }
    }

    partial void OnApiTypeChanged(SourceProvider oldValue, SourceProvider newValue)
    {
        if (oldValue != newValue)
        {
            Model.ApiType = newValue switch
            {
                SourceProvider.OpenAI => AIProvider.OpenAI,
                SourceProvider.Ollama => AIProvider.Ollama,
                _ => throw new NotSupportedException($"API provider '{newValue}' is not supported.")
            };
            SaveModel();
        }
    }

    partial void OnNameChanged(string oldValue, string newValue)
    {
        if (oldValue != newValue)
        {
            Model.Name = newValue;
            SaveModel();
        }
    }



    private void SaveModel()
    {
        App.GetService<IAIService>().Save();
    }

    [RelayCommand]
    public void Delete() { WeakReferenceMessenger.Default.Send(new ApiProviderDeletedMessage(this)); }

}
