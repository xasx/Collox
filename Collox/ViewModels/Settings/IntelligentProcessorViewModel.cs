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

public partial class IntelligentProcessorViewModel : ObservableObject
{
    [ObservableProperty] public partial ObservableCollection<string> AvailableModelIds { get; set; } = [];
    [ObservableProperty] public partial Guid FallbackId { get; set; }
    [ObservableProperty] public partial Guid Id { get; set; }
    [ObservableProperty] public partial string ModelId { get; set; }
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string Prompt { get; set; }
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
        Source = model.Provider switch
        {
            AIProvider.Ollama => SourceProvider.Ollama,
            AIProvider.OpenAI => SourceProvider.OpenAI,
            _ => throw new ArgumentOutOfRangeException(nameof(model.Provider), model.Provider, null)
        };
        Target = model.Target switch
        {
            Models.Target.Comment => ProcessorTarget.Comment,
            Models.Target.Task => ProcessorTarget.Task,
            Models.Target.Context => ProcessorTarget.Context,
            Models.Target.Chat => ProcessorTarget.Chat,
            _ => throw new ArgumentOutOfRangeException(nameof(model.Target), model.Target, null)
        };
        FallbackId = model.FallbackId;
    }

    [RelayCommand]
    public void Delete()
    {
        WeakReferenceMessenger.Default.Send(new ProcessorDeletedMessage(this));
    }

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
        App.GetService<AIService>().Config.Save();
    }

    partial void OnTargetChanged(ProcessorTarget value)
    {
        Model.Target = value switch
        {
            ProcessorTarget.Comment => Models.Target.Comment,
            ProcessorTarget.Task => Models.Target.Task,
            ProcessorTarget.Context => Models.Target.Context,
            ProcessorTarget.Chat => Models.Target.Chat,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        App.GetService<AIService>().Config.Save();
    }
    partial void OnModelIdChanged(string value)
    {
        Model.ModelId = value;
        App.GetService<AIService>().Config.Save();
    }

    partial void OnPromptChanged(string value)
    {
        Model.Prompt = value;
        App.GetService<AIService>().Config.Save();
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
        App.GetService<AIService>().Config.Save();
    }

    partial void OnFallbackIdChanged(Guid value)
    {
        Model.FallbackId = value;
        App.GetService<AIService>().Config.Save();
    }

    partial void OnIdChanged(Guid value)
    {
        Model.Id = value;
        App.GetService<AIService>().Config.Save();
    }
}

public enum ProcessorTarget
{
    Comment,
    Task,
    Context,
    Chat
}
