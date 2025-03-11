using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using NLog.Filters;

namespace Collox.ViewModels;

public partial class MirrorViewModel : ObservableRecipient, IRecipient<TextSubmittedMessage>
{
    private static readonly string All = "All";

    [ObservableProperty] public partial ObservableCollection<TextColloxMessage> Messages { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<TextColloxMessage> FilteredMessages { get; set; } = [];

    [ObservableProperty] public partial string SelectedContext { get; set; } = All;

    [ObservableProperty] public partial ObservableCollection<string> Contexts { get; set; } = [All];

    public MirrorViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(TextSubmittedMessage message)
    {
        Messages.Add(message.Value);
        if (!Contexts.Contains(message.Value.Context))
        {
            Contexts.Add(message.Value.Context);
        }
        if (SelectedContext == All || SelectedContext == message.Value.Context)
        {
            FilteredMessages.Add(message.Value);
        }
    }

    public void FilterMessages()
    {
        FilteredMessages.Clear();
        if (SelectedContext == All)
        {
            FilteredMessages.AddRange(Messages);
        }
        else
        {
            FilteredMessages.AddRange(Messages.Where(m => m.Context == SelectedContext));
        }
    }

    partial void OnSelectedContextChanged(string value)
    {
        FilterMessages();
    }

}
