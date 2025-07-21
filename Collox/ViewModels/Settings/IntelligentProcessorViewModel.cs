using System.Collections.ObjectModel;
using Collox.Models;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public enum SourceProvider
{
    Ollama,
    OpenAI
}

public partial class IntelligentProcessorViewModel : ObservableObject, IEquatable<IntelligentProcessorViewModel>
{
    public bool Equals(IntelligentProcessorViewModel other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    [ObservableProperty] public partial ObservableCollection<string> AvailableModelIds { get; set; } = [];

    [ObservableProperty] public partial Guid FallbackId { get; set; }

    [ObservableProperty] public partial Guid Id { get; set; }

    [ObservableProperty] public partial string ModelId { get; set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string Prompt { get; set; }

    [ObservableProperty] public partial string SystemPrompt { get; set; }

    [ObservableProperty] public partial SourceProvider Source { get; set; }

    [ObservableProperty] public partial ProcessorTarget Target { get; set; }

    [ObservableProperty] public partial string NamePresentation { get; set; } = "Edit";

    public IntelligentProcessor Model { get; init; }

    public IntelligentProcessorViewModel(IntelligentProcessor model)
    {
        Model = model;
        Id = model.Id;
        Name = model.Name;
        ModelId = model.ModelId;
        Prompt = model.Prompt;
        SystemPrompt = model.SystemPrompt;
        Source = model.Provider switch
        {
            AIProvider.Ollama => SourceProvider.Ollama,
            AIProvider.OpenAI => SourceProvider.OpenAI,
            _ => throw new ArgumentOutOfRangeException(nameof(model), $"Invalid provider: {model.Provider}")
        };
        Target = model.Target switch
        {
            Models.Target.Comment => ProcessorTarget.Comment,
            Models.Target.Task => ProcessorTarget.Task,
            Models.Target.Message => ProcessorTarget.Message,
            Models.Target.Chat => ProcessorTarget.Chat,
            _ => throw new ArgumentOutOfRangeException(nameof(model), $"Invalid target: {model.Target}")
        };
        FallbackId = model.FallbackId;
        AvailableModelIds.Add(ModelId);
    }

    [RelayCommand]
    public void Delete() { WeakReferenceMessenger.Default.Send(new ProcessorDeletedMessage(this)); }

    async partial void OnSourceChanged(SourceProvider value)
    {
        AvailableModelIds.Clear();
        switch (value)
        {
            case SourceProvider.Ollama:
                AvailableModelIds.AddRange(await AIModelHelpers.GetOllamaModels());
                Model.Provider = AIProvider.Ollama;
                break;

            case SourceProvider.OpenAI:
                AvailableModelIds.AddRange(await AIModelHelpers.GetOpenAIModels());
                Model.Provider = AIProvider.OpenAI;
                break;
        }
        SaveModel();
    }

    partial void OnTargetChanged(ProcessorTarget value)
    {
        Model.Target = value switch
        {
            ProcessorTarget.Comment => Models.Target.Comment,
            ProcessorTarget.Task => Models.Target.Task,
            ProcessorTarget.Message => Models.Target.Message,
            ProcessorTarget.Chat => Models.Target.Chat,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        SaveModel();
    }

    partial void OnModelIdChanged(string value)
    {
        Model.ModelId = value;
        SaveModel();
    }

    partial void OnPromptChanged(string value)
    {
        Model.Prompt = value;
        SaveModel();
    }

    partial void OnSystemPromptChanged(string value)
    {
        Model.SystemPrompt = value;
        SaveModel();
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
        SaveModel();
    }

    partial void OnFallbackIdChanged(Guid value)
    {
        Model.FallbackId = value;
        SaveModel();
    }

    partial void OnIdChanged(Guid value)
    {
        Model.Id = value;
        SaveModel();
    }

    private void SaveModel()
    {
        App.GetService<IAIService>().Save();
    }

    public override bool Equals(object obj) { return Equals(obj as IntelligentProcessorViewModel); }
}

public enum ProcessorTarget
{
    Comment,
    Task,
    Message,
    Chat
}
