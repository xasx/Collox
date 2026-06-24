using System.Collections.ObjectModel;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Serilog;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using NetworkHelper = CommunityToolkit.WinUI.Helpers.NetworkHelper;

namespace Collox.ViewModels;

public partial class MainViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<string>>,
    ITitleBarAutoSuggestBoxAware, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<MainViewModel>();
    private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IUserNotificationService _userNotificationService;
    private readonly IStoreService _storeService;
    private bool _disposed;

    public MainViewModel(
        IUserNotificationService userNotificationService,
        IStoreService storeService)
    {
        _userNotificationService = userNotificationService;
        _storeService = storeService;

        NetworkHelper.Instance.NetworkChanged += Instance_NetworkChanged;
    }

    [ObservableProperty] public partial bool IsAIEnabled { get; set; } = Settings.EnableAI;

    [ObservableProperty] public partial bool InternetState { get; set; } 

    [ObservableProperty] public partial ObservableCollection<UserNotification> UserNotifications { get; set; } = [];

    [ObservableProperty] public partial bool UserNotificationsEmpty { get; set; } = true;

    [ObservableProperty] public partial string DocumentFilename { get; set; }

    [ObservableProperty] public partial string ConfigurationLocation { get; set; } = Constants.AppConfigPath;

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

    partial void OnIsAIEnabledChanged(bool value) => Settings.EnableAI = value;

    [RelayCommand]
    public async Task InitAsync()
    {
        RefreshInternetState();
        await InitializeNotificationsAsync().ConfigureAwait(true);

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        DocumentFilename = _storeService.GetFilename();
    }

    /// <summary>
    ///     Initializes the notification listener. Failures here must never crash
    ///     the app (it runs on MainPage Loaded via a fire-and-forget command),
    ///     so every step is isolated and logged. The feature simply degrades to
    ///     an empty panel rather than opening the ErrorWindow.
    /// </summary>
    private async Task InitializeNotificationsAsync()
    {
        try
        {
            await _userNotificationService.Initialize().ConfigureAwait(true);
            _userNotificationService.OnUserNotificationsViewChanged +=
                UserNotificationService_OnUserNotificationsViewChanged;

            var userNotifications = await _userNotificationService.GetNotifications().ConfigureAwait(true);
            UserNotifications.AddRange(userNotifications);
            UserNotificationsEmpty = UserNotifications.Count == 0;
        }
        catch (Exception ex)
        {
            // Notification listening is best-effort: it depends on package identity
            // and a known-flaky event subscription (WindowsAppSDK #6172). Log and
            // continue rather than surfacing an unhandled exception.
            Logger.Error(ex, "Failed to initialize user notification listener; notifications disabled");
            UserNotificationsEmpty = true;
        }
    }

    [RelayCommand]
    public void ClearNotifications()
    {
        _userNotificationService.DismissAllNotifications();
        UserNotifications.Clear();
        UserNotificationsEmpty = true;
    }

    [RelayCommand]
    public async Task OpenDocumentFolderAsync()
    {
        var filename = _storeService.GetFilename();
        if (string.IsNullOrEmpty(filename))
            return;
        var folder = Path.GetDirectoryName(filename);
        if (folder == null)
            return;
        await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(folder));
    }

    [RelayCommand]
    public async Task OpenConfigurationFolderAsync()
    {
        var folder = Path.GetDirectoryName(ConfigurationLocation);
        if (folder == null)
            return;
        await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(folder));
    }

    private void Instance_NetworkChanged(object sender, EventArgs e) => dispatcherQueue.TryEnqueue(RefreshInternetState);

    private void RefreshInternetState() => InternetState = NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;

    private void UserNotificationService_OnUserNotificationsViewChanged(IReadOnlyList<UserNotification> newView) => dispatcherQueue.TryEnqueue(() =>
                                                                                                                         {
                                                                                                                             UserNotifications.Clear();
                                                                                                                             UserNotifications.AddRange(newView);
                                                                                                                             UserNotificationsEmpty = UserNotifications.Count == 0;
                                                                                                                         });

    public void Dispose()
    {
        if (_disposed)
            return;

        NetworkHelper.Instance.NetworkChanged -= Instance_NetworkChanged;

        if (_userNotificationService != null)
        {
            _userNotificationService.OnUserNotificationsViewChanged -= UserNotificationService_OnUserNotificationsViewChanged;
        }

        _disposed = true;
    }
}
