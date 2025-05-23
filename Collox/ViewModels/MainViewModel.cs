﻿using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Notifications;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NetworkHelper = CommunityToolkit.WinUI.Helpers.NetworkHelper;
using Windows.Storage;

namespace Collox.ViewModels;

public partial class MainViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<string>>,
    ITitleBarAutoSuggestBoxAware
{
    private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MainViewModel()
    {
        NetworkHelper.Instance.NetworkChanged += Instance_NetworkChanged;
    }

    [ObservableProperty] public partial bool IsAIEnabled { get; set; } = Settings.EnableAI;

    [ObservableProperty] public partial InternetState InternetState { get; set; } = new();

    [ObservableProperty] public partial ObservableCollection<UserNotification> UserNotifications { get; set; } = [];

    [ObservableProperty] public partial string DocumentFilename { get; set; }

    [ObservableProperty] public partial string ConfigurationLocation { get; set; } = Constants.AppConfigPath;

    private UserNotificationService UserNotificationService { get; } = App.GetService<UserNotificationService>();

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender.GetType().GetInterfaces().Contains(typeof(IStoreService)) &&
            message.PropertyName == "Filename")
        {
            DocumentFilename = message.NewValue;
        }
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
    }

    partial void OnIsAIEnabledChanged(bool value)
    {
        Settings.EnableAI = value;
    }

    [RelayCommand]
    public async Task Init()
    {
        RefreshInternetState();
        await UserNotificationService.Initialize();
        UserNotificationService.OnUserNotificationsViewChanged +=
            UserNotificationService_OnUserNotificationsViewChanged;

        var userNotifications = await UserNotificationService.GetNotifications();
        UserNotifications.AddRange(userNotifications);

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        DocumentFilename = App.GetService<IStoreService>().GetFilename();
    }

    [RelayCommand]
    public async Task OpenDocumentFolder()
    {
        var filename = App.GetService<IStoreService>().GetFilename();
        if (string.IsNullOrEmpty(filename))
            return;
        var folder = Path.GetDirectoryName(filename);
        if (folder == null)
            return;
        await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(folder));
    }

    [RelayCommand]
    public async Task OpenConfigurationFolder()
    {
        var folder = Path.GetDirectoryName(ConfigurationLocation);
        if (folder == null)
            return;
        await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(folder));
    }

    private void Instance_NetworkChanged(object sender, EventArgs e)
    {
        dispatcherQueue.TryEnqueue(RefreshInternetState);
    }

    private void RefreshInternetState()
    {
        if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
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

    private void UserNotificationService_OnUserNotificationsViewChanged(IReadOnlyList<UserNotification> newView)
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
    [ObservableProperty] public partial string Icon { get; set; } = "\uF384";

    [ObservableProperty] public partial string State { get; set; } = "offline";
}
