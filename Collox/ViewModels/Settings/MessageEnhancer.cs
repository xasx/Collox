using System.Collections.ObjectModel;

namespace Collox.ViewModels;

public partial class MessageEnhancer : ObservableObject
{
    public AISettingsViewModel ViewModelRef { get; init; }

    [ObservableProperty] public partial string Id { get; set; }

    [ObservableProperty] public partial bool IsEnabled { get; set; }

    [ObservableProperty] public partial string Prompt { get; set; }
    [ObservableProperty] public partial string ModelId { get; set; }
    [ObservableProperty] public partial EnhancerSource Source { get; set; }

    [ObservableProperty] public partial EnhancerTarget Target { get; set; }

    [ObservableProperty] public partial string FallbackEnhancerId { get; set; } = string.Empty;

    [ObservableProperty] public partial ObservableCollection<string> AvailableModelIds { get; set; } = [];

    partial void OnSourceChanged(EnhancerSource value)
    {
        AvailableModelIds.Clear();
        switch (value)
        {
            case EnhancerSource.Ollama:
                AvailableModelIds.AddRange(ViewModelRef.AvailableOllamaModelIds);
                break;
            case EnhancerSource.OpenAI:
                AvailableModelIds.AddRange(ViewModelRef.AvailableOpenAIModelIds);
                break;
        }
    }
}
public enum EnhancerSource
{
    Ollama,
    OpenAI,
}

public enum EnhancerTarget
{
    Comment,
    Task,

}
