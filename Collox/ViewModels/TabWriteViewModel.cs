using System.Collections.ObjectModel;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableObject
{
    private static readonly TabData initialTab = new()
    {
        Context = "Default",
        IsCloseable = false,
        IsEditing = false
    };

    private readonly Dictionary<TabData, TabContext> tabContexts = [];

    private readonly ITabContextService tabContextService = App.GetService<ITabContextService>();

    [ObservableProperty] public partial ObservableCollection<TabData> Contexts { get; set; } = [initialTab];

    [ObservableProperty] public partial TabData SelectedTab { get; set; } = initialTab;

    [RelayCommand]
    public void LoadTabs()
    {
        foreach (var tab in tabContextService.GetTabs())
        {
            var tabDataItem = new TabData
            {
                Context = tab.Name,
                IsCloseable = tab.IsCloseable,
                IsEditing = false
            };
            Contexts.Add(tabDataItem);
            tabContexts[tabDataItem] = tab;
        }
    }

    [RelayCommand]
    public void AddContext()
    {
        var context = $"Context {Contexts.Count + 1}";

        var newTabContext = new TabContext { Name = context, IsCloseable = true };
        var newTab = new TabData
        {
            Context = context,
            IsCloseable = true,
            IsEditing = true
        };
        Contexts.Add(newTab);
        SelectedTab = newTab;
        WeakReferenceMessenger.Default.Send(new FocusTabMessage(newTab));

        tabContextService.SaveNewTab(newTabContext);
        tabContexts[newTab] = newTabContext;
    }

    [RelayCommand]
    public void RemoveContext()
    {
        if (Contexts.Count > 1)
        {
            RemoveContext(SelectedTab);
        }
    }

    public void RemoveContext(TabData tabData)
    {
        Contexts.Remove(tabData);
        var tabContext = tabContexts[tabData];
        tabContexts.Remove(tabData);
        tabContextService.RemoveTab(tabContext);
    }

    public void UpdateContext(TabData tabData)
    {
        var tabContext = tabContexts[tabData];
        tabContext.Name = tabData.Context;
        tabContext.IsCloseable = tabData.IsCloseable;
        tabContextService.NotifyTabUpdate(tabContext);
    }
}

public partial class TabData : ObservableObject
{
    [ObservableProperty] public partial string Context { get; set; }

    [ObservableProperty] public partial bool IsCloseable { get; set; }

    [ObservableProperty] public partial bool IsEditing { get; set; }
}

public class FocusTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);
