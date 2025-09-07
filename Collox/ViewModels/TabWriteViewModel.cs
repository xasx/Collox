using System.Collections.ObjectModel;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;

namespace Collox.ViewModels;

public partial class TabWriteViewModel : ObservableRecipient, ITitleBarAutoSuggestBoxAware, IRecipient<UpdateTabMessage>, INavigationAwareEx
{
    private static readonly ILogger Logger = Log.ForContext<TabWriteViewModel>();
    private static readonly TabData initialTab = new() { Context = "Default", IsCloseable = false, IsEditing = false };

    private readonly Dictionary<TabData, TabContext> tabContexts = new()
    {
        [initialTab] = new TabContext { Name = initialTab.Context, IsCloseable = initialTab.IsCloseable }
    };

    private readonly ITabContextService tabContextService;
    private readonly IAIService aiService;

    public TabWriteViewModel(ITabContextService tabContextService, IAIService aiService)
    {
        this.tabContextService = tabContextService;
        this.aiService = aiService;

        WeakReferenceMessenger.Default.RegisterAll(this);
        Logger.Information("TabWriteViewModel initialized");
    }

    [ObservableProperty] public partial TabData SelectedTab { get; set; } = initialTab;

    [ObservableProperty] public partial ObservableCollection<TabData> Tabs { get; set; } = [initialTab];

    [RelayCommand]
    public void AddNewTab()
    {
        var context = $"Context {Tabs.Count + 1}";
        Logger.Information("Creating new tab with context: {Context}", context);

        var newTabContext = new TabContext { Name = context, IsCloseable = true, ActiveProcessors = [] };
        var newTabData = new TabData { Context = context, IsCloseable = true, IsEditing = true, ActiveProcessors = [] };

        Tabs.Add(newTabData);
        SelectedTab = newTabData;
        WeakReferenceMessenger.Default.Send(new FocusTabMessage(newTabData));

        tabContextService.SaveNewTab(newTabContext);
        tabContexts[newTabData] = newTabContext;

        Logger.Information("New tab created and saved: {Context}", context);
    }

    [RelayCommand]
    public void CloseSelectedTab()
    {
        if (Tabs.Count > 1)
        {
            Logger.Information("Closing selected tab: {Context}", SelectedTab.Context);
            RemoveTab(SelectedTab);
        }
        else
        {
            Logger.Debug("Cannot close last remaining tab");
        }
    }

    [RelayCommand]
    public void LoadTabs()
    {
        Logger.Information("Loading tabs from service");

        var procs = aiService.GetAll();
        var loadedTabs = tabContextService.GetTabs();

        Logger.Debug("Found {TabCount} tabs to load", loadedTabs.Count);

        foreach (var tabContext in loadedTabs)
        {
            if (tabContext.Name == initialTab.Context)
            {
                Logger.Debug("Updating initial tab with saved data");
                initialTab.ActiveProcessors.Clear();
                initialTab.ActiveProcessors.AddRange(
                    tabContext.ActiveProcessors.ConvertAll(x => procs.FirstOrDefault(p => p.Id == x)));
                tabContexts[initialTab] = tabContext;
                continue;
            }

            var tabData = new TabData
            {
                Context = tabContext.Name,
                IsCloseable = tabContext.IsCloseable,
                IsEditing = false
            };
            tabData.ActiveProcessors.AddRange(
                tabContext.ActiveProcessors.ConvertAll(x => procs.FirstOrDefault(p => p.Id == x)));

            Tabs.Add(tabData);
            tabContexts[tabData] = tabContext;

            Logger.Debug("Loaded tab: {Context}", tabContext.Name);
        }

        Logger.Information("Completed loading {TabCount} tabs", loadedTabs.Count);
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

    public void OnNavigatedFrom()
    {
        App.GetService<IStoreService>().SaveNow();
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void Receive(UpdateTabMessage message)
    {
        Logger.Debug("Received UpdateTabMessage for tab: {Context}", message.Value.Context);
        UpdateContext(message.Value);
    }

    public void RemoveTab(TabData tabData)
    {
        Logger.Information("Removing tab: {Context}", tabData.Context);

        Tabs.Remove(tabData);
        var tabContext = tabContexts[tabData];
        tabContexts.Remove(tabData);
        tabContextService.RemoveTab(tabContext);

        Logger.Information("Tab removed: {Context}", tabData.Context);
    }

    public void UpdateContext(TabData tabData)
    {
        Logger.Debug("Updating context for tab: {Context}", tabData.Context);

        var tabContext = tabContexts[tabData];
        tabContext.Name = tabData.Context;
        tabContext.IsCloseable = tabData.IsCloseable;
        tabContext.ActiveProcessors.Clear();
        tabContext.ActiveProcessors.AddRange(tabData.ActiveProcessors.Select(x => x.Id));
        tabContextService.NotifyTabUpdate(tabContext);

        Logger.Debug("Context updated for tab: {Context}", tabData.Context);
    }
}
