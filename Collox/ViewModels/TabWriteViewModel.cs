using System.Collections.ObjectModel;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private static readonly TabData initialTab = new()
    {
        Context = "Default",
        IsCloseable = false,
        IsEditing = false
    };

    private readonly Dictionary<TabData, TabContext> tabContexts = new() {
        [initialTab] = new TabContext { Name = initialTab.Context, IsCloseable = initialTab.IsCloseable }
    };

    private readonly ITabContextService tabContextService = App.GetService<ITabContextService>();

    [ObservableProperty] public partial ObservableCollection<TabData> Tabs { get; set; } = [initialTab];

    [ObservableProperty] public partial TabData SelectedTab { get; set; } = initialTab;

    [RelayCommand]
    public void LoadTabs()
    {
        foreach (var tabContext in tabContextService.GetTabs())
        {
            var tabData = new TabData
            {
                Context = tabContext.Name,
                IsCloseable = tabContext.IsCloseable,
                IsEditing = false
            };
            Tabs.Add(tabData);
            tabContexts[tabData] = tabContext;
        }
    }

    [RelayCommand]
    public void AddNewTab()
    {
        var context = $"Context {Tabs.Count + 1}";

        var newTabContext = new TabContext { Name = context, IsCloseable = true };
        var newTabData = new TabData { Context = context, IsCloseable = true, IsEditing = true };

        Tabs.Add(newTabData);
        SelectedTab = newTabData;
        WeakReferenceMessenger.Default.Send(new FocusTabMessage(newTabData));

        tabContextService.SaveNewTab(newTabContext);
        tabContexts[newTabData] = newTabContext;
    }

    [RelayCommand]
    public void CloseSelectedTab()
    {
        if (Tabs.Count > 1)
        {
            RemoveTab(SelectedTab);
        }
    }

    public void RemoveTab(TabData tabData)
    {
        Tabs.Remove(tabData);
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

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // find tabframe in the tab.
        var frame = WeakReferenceMessenger.Default.Send< GetFrameRequestMessage>();
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, frame);
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var frame = WeakReferenceMessenger.Default.Send<GetFrameRequestMessage>();
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, frame);
    }
}

public partial class TabData : ObservableObject
{
    [ObservableProperty] public partial string Context { get; set; }

    [ObservableProperty] public partial bool IsCloseable { get; set; }

    [ObservableProperty] public partial bool IsEditing { get; set; }
}

public class FocusTabMessage(TabData tabData) : ValueChangedMessage<TabData>(tabData);

public class  GetFrameRequestMessage : RequestMessage<Frame>
{
}
