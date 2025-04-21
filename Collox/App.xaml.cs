using System.Diagnostics;
using Collox.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Collox;

public partial class App : Application
{
    public static Window MainWindow;
    public static Window MirrorWindow;

    public App()
    {
        Services = ConfigureServices();
        UnhandledException += Application_UnhandledException;
        InitializeComponent();

        System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
        System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
    }

    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public IJsonNavigationService GetNavService => GetService<IJsonNavigationService>();
    public IThemeService GetThemeService => GetService<IThemeService>();

    private bool isClosing;

    public static T GetService<T>() where T : class
    {
        if (Current!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IJsonNavigationService, JsonNavigationService>();

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

        services.AddSingleton<IStoreService, StoreService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<UserNotificationService>();
        services.AddSingleton<ITabContextService, TabContextService>();
        services.AddSingleton<AIApis>();
        services.AddSingleton<AIService>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();

        var notificationManager = AppNotificationManager.Default;
        notificationManager.NotificationInvoked += NotificationManager_NotificationInvoked;
        notificationManager.Register();

        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var activationKind = activatedArgs.Kind;
        if (activationKind != ExtendedActivationKind.AppNotification)
        {
            SetupMainWindow();
            SetupMirrorWindow();
        }
        else
        {
            HandleNotification((AppNotificationActivatedEventArgs)activatedArgs.Data);
        }
    }

    // ...

    private void SetupMirrorWindow()
    {
        if (MirrorWindow == null)
        {
            MirrorWindow = new ModernWindow()
            {
                BackdropType = BackdropType.AcrylicThin,
                HasTitleBar = false,
                UseModernSystemMenu = true,
                SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin)
            };
        }

        if (MirrorWindow.Content is not Frame rootFrame)
        {
            MirrorWindow.Content = rootFrame = new Frame();
        }

        GetThemeService?.AutoInitialize(MirrorWindow);
        rootFrame.Navigate(typeof(MirrorPage));

        MirrorWindow.SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
        MirrorWindow.Title = MirrorWindow.AppWindow.Title = $"{ProcessInfoHelper.ProductName} - Mirror";
        MirrorWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
        MirrorWindow.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent | ExtendedWindowStyle.TopMost |
                                            ExtendedWindowStyle.NoInheritLayout);

        MirrorWindow.Closed += (sender, args) =>
        {
            if (isClosing) return;
            MirrorWindow.Hide();
            args.Handled = true;
        };
        MirrorWindow.SetIsAlwaysOnTop(true);
        MirrorWindow.SetIsMaximizable(false);
        MirrorWindow.SetIsMinimizable(false);
        MirrorWindow.SetIsResizable(false);
        MirrorWindow.SetIsShownInSwitchers(false);
    }

    private void NotificationManager_NotificationInvoked(AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        HandleNotification(args);
    }

    private void HandleNotification(AppNotificationActivatedEventArgs data)
    {
        var dispatcherQueue = MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();


        dispatcherQueue.TryEnqueue(async delegate
        {
            await Task.CompletedTask;
        });
    }

    private void SetupMainWindow()
    {
        if (MainWindow == null)
        {
            MainWindow = new Window();
        }

        if (MainWindow.Content is not Frame rootFrame)
        {
            MainWindow.Content = rootFrame = new Frame();
        }

        GetThemeService?.AutoInitialize(MainWindow);

        rootFrame.Navigate(typeof(MainPage));

        MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
        var msm = new ModernSystemMenu(MainWindow);


        MainWindow.Activate();
        MainWindow.SetForegroundWindow();
        MainWindow.Show();

        MainWindow.Closed += MainWindow_Closed;
        MainWindow.VisibilityChanged += MainWindow_VisibilityChanged;
    }

    private void MainWindow_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        if (!args.Visible)
        {
            MirrorWindow.Show();
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        await GetService<IStoreService>().SaveNow();
        Interlocked.Exchange(ref isClosing, true);
        MirrorWindow.Close();
    }

    private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"An error {e.Exception.Message}{Environment.NewLine}{e.Exception}");

        var errorWindow = new ErrorWindow
        {
            ReportedException = e.Exception
        };
        errorWindow.Show();
        e.Handled = true;
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
