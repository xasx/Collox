using System.Collections.ObjectModel;
using Collox.Models;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableRecipient, ITitleBarAutoSuggestBoxAware, IRecipient<UpdateTabMessage>
{
    private static readonly TabData initialTab = new() { Context = "Default", IsCloseable = false, IsEditing = false };

    private readonly Dictionary<TabData, TabContext> tabContexts = new()
    {
        [initialTab] = new TabContext { Name = initialTab.Context, IsCloseable = initialTab.IsCloseable }
    };

    private readonly ITabContextService tabContextService;

    public TabWriteViewModel(ITabContextService tabContextService)
    {
        this.tabContextService = tabContextService;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    [ObservableProperty] public partial TabData SelectedTab { get; set; } = initialTab;

    [ObservableProperty] public partial ObservableCollection<TabData> Tabs { get; set; } = [initialTab];

    [RelayCommand]
    public void AddNewTab()
    {
        var context = $"Context {Tabs.Count + 1}";

        var newTabContext = new TabContext { Name = context, IsCloseable = true, ActiveProcessors = [] };
        var newTabData = new TabData { Context = context, IsCloseable = true, IsEditing = true, ActiveProcessors = [] };

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

    [RelayCommand]
    public void LoadTabs()
    {
        var procs = App.GetService<AIService>().GetAll();
        foreach (var tabContext in tabContextService.GetTabs())
        {
            if (tabContext.Name == initialTab.Context)
            {
                initialTab.ActiveProcessors =
                    tabContext.ActiveProcessors.ConvertAll(x => procs.FirstOrDefault(p => p.Id == x));
                tabContexts[initialTab] = tabContext;
                continue;
            }

            var tabData = new TabData
            {
                Context = tabContext.Name,
                IsCloseable = tabContext.IsCloseable,
                IsEditing = false,
                ActiveProcessors = tabContext.ActiveProcessors.ConvertAll(x => procs.FirstOrDefault(p => p.Id == x))
            };
            Tabs.Add(tabData);
            tabContexts[tabData] = tabContext;
        }
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var frame = WeakReferenceMessenger.Default.Send<GetFrameRequestMessage>();
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, frame);
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // find tabframe in the tab.
        var frame = WeakReferenceMessenger.Default.Send<GetFrameRequestMessage>();
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, frame);
    }

    public void Receive(UpdateTabMessage message) { UpdateContext(message.Value); }

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
        tabContext.ActiveProcessors = tabData.ActiveProcessors.ConvertAll(x => x.Id);
        tabContextService.NotifyTabUpdate(tabContext);
    }
}
