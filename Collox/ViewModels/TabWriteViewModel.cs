using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableObject
{
    private static readonly TabData initialTab = new()
    {
        Context = "Default",
        IsCloseable = false,
        IsEditing = false,
    };

    [ObservableProperty]
    public partial ObservableCollection<TabData> Contexts { get; set; }
        = [initialTab];

    [ObservableProperty] public partial TabData SelectedTab { get; set; } = initialTab;

    [RelayCommand]
    public void AddContext()
    {
        var context = $"Context {Contexts.Count + 1}";
        var newTab = new TabData()
        {
            Context = context,
            IsCloseable = true,
            IsEditing = true
        };
        Contexts.Add(newTab);
        SelectedTab = newTab;
        WeakReferenceMessenger.Default.Send(new FocusTabMessage(newTab));
    }

    [RelayCommand]
    public void RemoveContext()
    {
        if (Contexts.Count > 1)
        {
            Contexts.Remove(SelectedTab);
        }
    }
}

public partial class TabData : ObservableObject
{
    [ObservableProperty]
    public partial string Context { get; set; }

    public bool IsCloseable { get; init; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }
}

public class FocusTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
