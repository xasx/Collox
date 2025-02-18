using System.Diagnostics;
using Collox.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace Collox;

public partial class App : Application
{
    public static Window MainWindow;
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public IJsonNavigationService GetNavService => GetService<IJsonNavigationService>();
    public IThemeService GetThemeService => GetService<IThemeService>();

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        Services = ConfigureServices();
        UnhandledException += Application_UnhandledException;
        InitializeComponent();
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

        services.AddTransient<WriteViewModel>();
        services.AddTransient<TemplatesViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<TabWriteViewModel>();

        services.AddSingleton<IStoreService, StoreService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<UserNotificationService>();
        services.AddSingleton<ITabContextService, TabContextService>();

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
        }
        else
        {
            HandleNotification((AppNotificationActivatedEventArgs)activatedArgs.Data);
        }
    }

    private void NotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
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

        MainWindow.Activate();
        WindowHelper.ShowWindow(MainWindow);

        MainWindow.Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
       await GetService<IStoreService>().SaveNow();
    }

    private void Application_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"An error {e.Exception.Message}{Environment.NewLine}{e.Exception}");
        //MessageBox.Show(WinRT.Interop.WindowNative.GetWindowHandle(MainWindow),
        //$"{e.Exception.Message}{Environment.NewLine}{e}", "Error", MessageBoxStyle.ApplicationModal | MessageBoxStyle.IconError | MessageBoxStyle.Ok);

        ErrorWindow errorWindow = new ErrorWindow
        {
            ReportedException = e.Exception
        };
        errorWindow.Show();
    }
}

internal static class WindowHelper
{

    public static void ShowWindow(Window window)
    {
        // Bring the window to the foreground... first get the window handle...
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(window));

        // Restore window if minimized... requires DLL import above
        PInvoke.ShowWindow(hwnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);

        // And call SetForegroundWindow... requires DLL import above
        PInvoke.SetForegroundWindow(hwnd);
    }
}
