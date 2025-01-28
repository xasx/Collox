using System.Collections.ObjectModel;
using System.Timers;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.System;
using Windows.UI.Notifications;

namespace Collox.ViewModels;
public partial class MainViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<string>>, ITitleBarAutoSuggestBoxAware
{
    private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MainViewModel()
    {
        CommunityToolkit.WinUI.Helpers.NetworkHelper.Instance.NetworkChanged += Instance_NetworkChanged;
    }

    [ObservableProperty]
    public partial InternetState InternetState { get; set; } = new InternetState();

    [ObservableProperty]
    public partial ObservableCollection<UserNotification> UserNotifications { get; set; } = [];

    [ObservableProperty]
    public partial string DocumentFilename { get; set; }

    private UserNotificationService UserNotificationService { get; } = App.GetService<UserNotificationService>();

    [RelayCommand]
    public async Task Init()
    {
        RefreshInternetState();
        await UserNotificationService.Initialize();
        UserNotificationService.OnUserNotificationsViewChanged += UserNotificationService_OnUserNotificationsViewChanged;

        var notifs = await UserNotificationService.GetNotifications();
            UserNotifications.AddRange(notifs);

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        DocumentFilename = App.GetService<IStoreService>().GetFilename();
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {

    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender.GetType().GetInterfaces().Contains(typeof(IStoreService)) &&
            message.PropertyName == "Filename")
        {
            DocumentFilename = message.NewValue;
        }

    }

    private void Instance_NetworkChanged(object sender, EventArgs e)
    {
        dispatcherQueue.TryEnqueue(() =>
        {
            RefreshInternetState();
        });
    }

    private void RefreshInternetState()
    {
        if (CommunityToolkit.WinUI.Helpers.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
        {
            InternetState.State = "online";
            InternetState.Icon = "\uE774";
        }
        else
        {
            InternetState.State = "offline";
            InternetState.Icon = "\uF384";
        }

    }
    private void UserNotificationService_OnUserNotificationsViewChanged(IReadOnlyList<Windows.UI.Notifications.UserNotification> newView)
    {
        dispatcherQueue.TryEnqueue(() =>
        {
            UserNotifications.Clear();
            UserNotifications.AddRange(newView);
        });
    }
}

public partial class InternetState : ObservableObject
{
    [ObservableProperty]
    public partial string Icon { get; set; } = "\uF384";

    [ObservableProperty]
    public partial string State { get; set; } = "offline";
}
