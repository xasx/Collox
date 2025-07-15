using Collox.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Collox;

public partial class App : Application
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Window MainWindow;
    public static Window MirrorWindow;

    public App()
    {
        Logger.Info("Initializing Collox application");

        try
        {
            Logger.Debug("Configuring dependency injection services");
            Services = ConfigureServices();
            Logger.Info("Dependency injection services configured successfully");

            Logger.Debug("Subscribing to unhandled exception events");
            UnhandledException += Application_UnhandledException;

            Logger.Debug("Initializing XAML components");
            InitializeComponent();

            Logger.Debug("Setting up profile optimization with root path: {RootPath}", Constants.RootDirectoryPath);
            System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
            System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");

            Logger.Info("Collox application initialization completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Critical error during application initialization");
            throw;
        }
    }

    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public IJsonNavigationService GetNavService => GetService<IJsonNavigationService>();
    public IThemeService GetThemeService => GetService<IThemeService>();

    private bool isClosing;

    public static T GetService<T>() where T : class
    {
        Logger.Trace("Requesting service of type: {ServiceType}", typeof(T).Name);

        try
        {
            if (Current!.Services.GetService(typeof(T)) is not T service)
            {
                Logger.Error("Service {ServiceType} is not registered in dependency injection container", typeof(T).Name);
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            Logger.Trace("Successfully resolved service: {ServiceType}", typeof(T).Name);
            return service;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to resolve service: {ServiceType}", typeof(T).Name);
            throw;
        }
    }

    private static ServiceProvider ConfigureServices()
    {
        Logger.Debug("Beginning service configuration");
        var services = new ServiceCollection();

        try
        {
            Logger.Trace("Registering singleton services");
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IJsonNavigationService, JsonNavigationService>();
            services.AddSingleton<IStoreService, StoreService>();
            services.AddSingleton<ITemplateService, TemplateService>();
            services.AddSingleton<IUserNotificationService, UserNotificationService>();
            services.AddSingleton<ITabContextService, TabContextService>();
            services.AddSingleton<AIApis>();
            services.AddSingleton<IAIService, AIService>();

            Logger.Trace("Registering transient view models");
            services.AddTransient<MainViewModel>();
            services.AddTransient<GeneralSettingViewModel>();
            services.AddTransient<AppUpdateSettingViewModel>();
            services.AddTransient<AboutUsSettingViewModel>();
            services.AddTransient<AISettingsViewModel>();
            services.AddTransient<WriteViewModel>();
            services.AddTransient<TemplatesViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<TabWriteViewModel>();
            services.AddTransient<MirrorViewModel>();

            var serviceProvider = services.BuildServiceProvider();
            Logger.Info("Service configuration completed. Total services registered: {ServiceCount}", services.Count);
            return serviceProvider;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to configure dependency injection services");
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.Info("Application launched with arguments: {Arguments}", args.Arguments);

        try
        {
            Logger.Debug("Creating main window instance");
            MainWindow = new Window();

            Logger.Debug("Setting up notification manager");
            var notificationManager = AppNotificationManager.Default;
            notificationManager.NotificationInvoked += NotificationManager_NotificationInvoked;
            notificationManager.Register();
            Logger.Info("Notification manager registered successfully");

            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var activationKind = activatedArgs.Kind;
            Logger.Debug("Processing activation kind: {ActivationKind}", activationKind);

            if (activationKind != ExtendedActivationKind.AppNotification)
            {
                Logger.Info("Standard activation - setting up main window and mirror window");
                SetupMainWindow();
                SetupMirrorWindow();
                Logger.Info("Application windows setup completed");
            }
            else
            {
                Logger.Info("App notification activation detected");
                HandleNotification((AppNotificationActivatedEventArgs)activatedArgs.Data);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Critical error during application launch");
            throw;
        }
    }

    private void SetupMirrorWindow()
    {
        Logger.Debug("Setting up mirror window");

        try
        {
            if (MirrorWindow == null)
            {
                Logger.Trace("Creating new ModernWindow for mirror");
                MirrorWindow = new ModernWindow()
                {
                    BackdropType = BackdropType.AcrylicThin,
                    HasTitleBar = false,
                    UseModernSystemMenu = true,
                    SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin)
                };
                Logger.Debug("Mirror window instance created");
            }

            if (MirrorWindow.Content is not Frame rootFrame)
            {
                Logger.Trace("Creating root frame for mirror window");
                MirrorWindow.Content = rootFrame = new Frame();
            }

            Logger.Trace("Initializing theme service for mirror window");
            GetThemeService?.AutoInitialize(MirrorWindow);

            Logger.Trace("Navigating to MirrorPage");
            rootFrame.Navigate(typeof(MirrorPage));

            MirrorWindow.SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
            MirrorWindow.Title = MirrorWindow.AppWindow.Title = $"{ProcessInfoHelper.ProductName} - Mirror";
            MirrorWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

            Logger.Debug("Setting extended window styles for mirror window");
            MirrorWindow.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent | ExtendedWindowStyle.TopMost |
                                                ExtendedWindowStyle.NoInheritLayout);

            MirrorWindow.Closed += (sender, args) =>
            {
                Logger.Debug("Mirror window close event triggered. IsClosing: {IsClosing}", isClosing);
                if (isClosing) return;
                Logger.Info("Hiding mirror window instead of closing");
                MirrorWindow.Hide();
                args.Handled = true;
            };

            Logger.Trace("Configuring mirror window properties");
            MirrorWindow.SetIsAlwaysOnTop(true);
            MirrorWindow.SetIsMaximizable(false);
            MirrorWindow.SetIsMinimizable(false);
            MirrorWindow.SetIsResizable(false);
            MirrorWindow.SetIsShownInSwitchers(false);

            Logger.Info("Mirror window setup completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to setup mirror window");
            throw;
        }
    }

    private void NotificationManager_NotificationInvoked(AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        Logger.Info("Notification invoked with user input: {UserInput}", args.UserInput?.Count ?? 0);
        HandleNotification(args);
    }

    private void HandleNotification(AppNotificationActivatedEventArgs data)
    {
        Logger.Debug("Handling notification with arguments: {Arguments}", data.Arguments?.Count ?? 0);

        try
        {
            var dispatcherQueue = MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
            Logger.Trace("Using dispatcher queue for notification handling");

            dispatcherQueue.TryEnqueue(async delegate
            {
                Logger.Trace("Executing notification handler on UI thread");
                await Task.CompletedTask;
                Logger.Debug("Notification handling completed");
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred while handling notification");
        }
    }

    private void SetupMainWindow()
    {
        Logger.Debug("Setting up main window");

        try
        {
            if (MainWindow == null)
            {
                Logger.Trace("Creating new main window instance");
                MainWindow = new Window();
            }

            if (MainWindow.Content is not Frame rootFrame)
            {
                Logger.Trace("Creating root frame for main window");
                MainWindow.Content = rootFrame = new Frame();
            }

            Logger.Trace("Initializing theme service for main window");
            GetThemeService?.AutoInitialize(MainWindow);

            Logger.Trace("Navigating to MainPage");
            rootFrame.Navigate(typeof(MainPage));

            MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
            MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

            Logger.Trace("Creating modern system menu");
            var msm = new ModernSystemMenu(MainWindow);

            Logger.Debug("Activating and showing main window");
            MainWindow.Activate();
            MainWindow.SetForegroundWindow();
            MainWindow.Show();

            Logger.Trace("Subscribing to main window events");
            MainWindow.Closed += MainWindow_Closed;
            MainWindow.VisibilityChanged += MainWindow_VisibilityChanged;

            Logger.Info("Main window setup completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to setup main window");
            throw;
        }
    }

    private void MainWindow_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        if (isClosing)
        {
            Logger.Debug("Main window visibility change ignored due to closing state");
            return;
        }

        Logger.Debug("Main window visibility changed. Visible: {IsVisible}", args.Visible);

        try
        {
            if (!args.Visible)
            {
                Logger.Info("Main window hidden, showing mirror window");
                MirrorWindow.Show();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred during main window visibility change handling");
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Logger.Info("Main window closing event triggered");

        try
        {
            Logger.Debug("Saving application state before closing");
            GetService<IStoreService>().SaveNow().Wait();
            Logger.Info("Application state saved successfully");

            Logger.Debug("Setting application closing flag");
            Interlocked.Exchange(ref isClosing, true);

            Logger.Debug("Closing mirror window");
            MirrorWindow.Close();

            Logger.Info("Application shutdown sequence completed");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred during application shutdown");
        }
    }

    private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Fatal(e.Exception, "Unhandled application exception occurred");

        try
        {
            Logger.Debug("Creating error window for unhandled exception");
            var errorWindow = new ErrorWindow
            {
                ReportedException = e.Exception
            };
            errorWindow.Show();
            e.Handled = true;
            Logger.Info("Error window displayed for unhandled exception");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to display error window for unhandled exception");
        }
    }
}

internal static class WindowHelper
{
    public static void ShowWindow(Window window)
    {
        var hwnd = new HWND(WindowNative.GetWindowHandle(window));

        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

        PInvoke.SetForegroundWindow(hwnd);

    }
}
