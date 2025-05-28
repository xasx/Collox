using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public partial class TaskViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsDone { get; set; }

    public static implicit operator TaskViewModel(string task)
    {
        return new TaskViewModel { Name = task, IsDone = false };
    }

    partial void OnIsDoneChanged(bool value)
    {
        if (value)
        {
            WeakReferenceMessenger.Default.Send(new TaskDoneMessage(this));
        }
    }
}
