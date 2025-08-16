using Collox.Models;
using Collox.Services;
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

    partial void OnApiKeyChanged(string value)
    {
        Model.ApiKey = value;
        SaveModel();
    }

    partial void OnEndpointChanged(string value)
    {
        Model.Endpoint = value;
        SaveModel();
    }

    partial void OnApiTypeChanged(SourceProvider value)
    {
        Model.ApiType = value switch
        {
            SourceProvider.OpenAI => AIProvider.OpenAI,
            SourceProvider.Ollama => AIProvider.Ollama,
            _ => throw new NotSupportedException($"API provider '{value}' is not supported.")
        };
        SaveModel();
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
        SaveModel();
    }



    private void SaveModel()
    {
        App.GetService<IAIService>().Save();
    }

    [RelayCommand]
    public void Delete() { WeakReferenceMessenger.Default.Send(new ApiProviderDeletedMessage(this)); }

}
