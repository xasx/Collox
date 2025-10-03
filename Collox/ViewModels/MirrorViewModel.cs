using System.Collections.ObjectModel;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class MirrorViewModel : ObservableRecipient, IRecipient<TextSubmittedMessage>, IDisposable
{
    private const string All = "All";
    private bool _disposed;

    [ObservableProperty] public partial ObservableCollection<TextColloxMessage> Messages { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<TextColloxMessage> FilteredMessages { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<string> SelectedContexts { get; set; } = [All];

    [ObservableProperty] public partial ObservableCollection<string> Contexts { get; set; } = [All];

    public MirrorViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    [RelayCommand]
    public void Clear()
    {
        // Messages.Clear();
        FilteredMessages.Clear();
    }

    public void Receive(TextSubmittedMessage message)
    {
        Messages.Add(message.Value);
        // Limit messages to 20
        if (Messages.Count > 20)
        {
            Messages.RemoveAt(0);
        }

        if (!Contexts.Contains(message.Value.Context))
        {
            Contexts.Add(message.Value.Context);
        }

        if (SelectedContexts.Contains(All) || SelectedContexts.Contains(message.Value.Context))
        {
            FilteredMessages.Add(message.Value);
            if (FilteredMessages.Count > 20)
            {
                FilteredMessages.RemoveAt(0);
            }
        }
    }

    public void FilterMessages()
    {
        FilteredMessages.Clear();
        FilteredMessages.AddRange(SelectedContexts.Contains(All)
            ? Messages
            : Messages.Where(m => SelectedContexts.Contains(m.Context)));
    }

    partial void OnSelectedContextsChanged(ObservableCollection<string> value)
    {
        FilterMessages();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<TextSubmittedMessage>(this);
        _disposed = true;
    }
}
