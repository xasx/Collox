using System.Collections.ObjectModel;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableObject
{

    private static readonly TabData initialTab = new TabData("Default", false);
    [ObservableProperty]
    public partial ObservableCollection<TabData> Contexts { get; set; }
        = [initialTab];

    [ObservableProperty]
    public partial TabData SelectedTab { get; set; } = initialTab;

    [RelayCommand]
    public void AddContext()
    {
        var context = $"Context {Contexts.Count + 1}";
        var newTab = new TabData(context, true);
        Contexts.Add(newTab);
        SelectedTab = newTab;
    }
}

public record TabData(string Context, bool IsCloseable);
