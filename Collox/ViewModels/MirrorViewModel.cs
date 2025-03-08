using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;

namespace Collox.ViewModels;

public partial class MirrorViewModel : ObservableRecipient, IRecipient<TextSubmittedMessage>
{
    [ObservableProperty] public partial ObservableCollection<TextColloxMessage> Messages { get; set; } = [];

    public MirrorViewModel()
    {
        WeakReferenceMessenger.Default.Register<TextSubmittedMessage>(this);
    }

    public void Receive(TextSubmittedMessage message)
    {
        Messages.Add(message.Value);
    }
}
