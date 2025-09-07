using Collox.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Serilog;
using WinUIEx;
using ILogger = Serilog.ILogger;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Collox;

public partial class App : Application
{
    public static ILogger Logger { get; private set; }

    public static Window MainWindow;
    private static readonly Lazy<Window> _lazyMirrorWindow = new(CreateMirrorWindow);
    public static Window MirrorWindow => _lazyMirrorWindow.Value;

    // Lazy service provider for better startup performance
    private readonly Lazy<IServiceProvider> _lazyServices;

    public App()
    {
        // Initialize Serilog first, before any logging calls
        InitializeSerilog();

        Logger.Information("Initializing Collox application");

        try
        {
            // Defer service configuration until first access
            _lazyServices = new Lazy<IServiceProvider>(ConfigureServices);

            Logger.Debug("Subscribing to unhandled exception events");
            UnhandledException += Application_UnhandledException;

            Logger.Debug("Initializing XAML components");
            InitializeComponent();

            // Move profile optimization to background thread
            _ = Task.Run(() =>
            {
                Logger.Debug("Setting up profile optimization with root path: {RootPath}", Constants.RootDirectoryPath);
                System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
                System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
            });

            Logger.Information("Collox application initialization completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Critical error during application initialization");
            throw;
        }
    }

    private static void InitializeSerilog()
    {
        // Get a proper logs directory
        var logsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Collox",
            "logs",
            "collox-.log");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logsPath));

        // Configure Serilog with programmatic file path
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logsPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Collox")
            .CreateLogger();

        // Create a logger specifically for this class
        Logger = Log.ForContext<App>();
    }

    public IServiceProvider Services => _lazyServices.Value;
    public new static App Current => (App)Application.Current;
    public INavigationServiceEx GetNavService => GetService<INavigationServiceEx>();
    public IThemeService GetThemeService => GetService<IThemeService>();

    private bool isClosing;

    public static T GetService<T>() where T : class
    {
        try
        {
            if (Current!.Services.GetService(typeof(T)) is not T service)
            {
                Logger.Error("Service {ServiceType} is not registered in dependency injection container", typeof(T).Name);
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

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
            // Add Serilog as the logging provider
            services.AddLogging(builder => builder.AddSerilog());

            // Register core services first (these are needed immediately)
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<INavigationServiceEx, NavigationServiceEx>();
            services.AddSingleton<IStoreService, StoreService>();

            services.AddSingleton<IAIService, AIService>();

            // Other services that can be deferred
            services.AddSingleton<ITemplateService, TemplateService>();
            services.AddSingleton<IUserNotificationService, UserNotificationService>();
            services.AddSingleton<ITabContextService, TabContextService>();
            services.AddSingleton<ICommandService, CommandService>();
            services.AddSingleton<IMessageProcessingService, MessageProcessingService>();
            services.AddSingleton<IAudioService, AudioService>();

            // Register view models as transient (created when needed)
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
            Logger.Information("Service configuration completed. Total services registered: {ServiceCount}", services.Count);
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
        Logger.Information("Application launched with arguments: {Arguments}", args.Arguments);

        try
        {
            // Create main window immediately but defer heavy setup
            Logger.Debug("Creating main window instance");
            MainWindow = new Window();

            // Setup notification manager on background thread
            _ = Task.Run(() =>
            {
                try
                {
                    Logger.Debug("Setting up notification manager");
                    var notificationManager = AppNotificationManager.Default;
                    notificationManager.NotificationInvoked += NotificationManager_NotificationInvoked;
                    notificationManager.Register();
                    Logger.Information("Notification manager registered successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to setup notification manager");
                }
            });

            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var activationKind = activatedArgs.Kind;
            Logger.Debug("Processing activation kind: {ActivationKind}", activationKind);

            if (activationKind != ExtendedActivationKind.AppNotification)
            {
                Logger.Information("Standard activation - setting up main window");
                SetupMainWindow();
                Logger.Information("Main window setup completed");
            }
            else
            {
                Logger.Information("App notification activation detected");
                HandleNotification((AppNotificationActivatedEventArgs)activatedArgs.Data);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Critical error during application launch");
            throw;
        }
    }

    private static Window CreateMirrorWindow()
    {
        Logger.Debug("Creating mirror window via lazy initialization");

        try
        {
            var mirrorWindow = new ModernWindow()
            {
                BackdropType = BackdropType.AcrylicThin,
                HasTitleBar = false,
                UseModernSystemMenu = true,
                SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin)
            };

            var rootFrame = new Frame();
            mirrorWindow.Content = rootFrame;

            Current.GetThemeService?.AutoInitialize(mirrorWindow);
            rootFrame.Navigate(typeof(MirrorPage));
            mirrorWindow.ExtendsContentIntoTitleBar = true;

            mirrorWindow.SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
            mirrorWindow.Title = mirrorWindow.AppWindow.Title = $"{ProcessInfoHelper.ProductName} - Mirror";
            mirrorWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

            mirrorWindow.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent | ExtendedWindowStyle.TopMost |
                                               ExtendedWindowStyle.NoInheritLayout);

            mirrorWindow.Closed += (sender, args) =>
            {
                if (Current.isClosing) return;
                Logger.Information("Hiding mirror window instead of closing");
                mirrorWindow.Hide();
                args.Handled = true;
            };

            mirrorWindow.SetIsAlwaysOnTop(true);
            mirrorWindow.SetIsMaximizable(false);
            mirrorWindow.SetIsMinimizable(false);
            mirrorWindow.SetIsResizable(false);
            mirrorWindow.SetIsShownInSwitchers(false);

            var appNotification = new AppNotificationBuilder()
                .AddArgument("action", "ToastClick")
                //.AddArgument(Common.scenarioTag, ScenarioId.ToString())
                .SetAppLogoOverride(new System.Uri("ms-appx:///Assets/Fluent/Collox.png"),
                AppNotificationImageCrop.Default)
                //.AddText(ScenarioName)
                .AddText("Showing mirror windowwhile in the background.")
                .AddText("Click to open main window.")
                .AddButton(new AppNotificationButton("Open Main Window")
                    .AddArgument("action", "OpenApp")
                    //.AddArgument(Common.scenarioTag, ScenarioId.ToString())
                    )
                .BuildNotification();

            AppNotificationManager.Default.Show(appNotification);

            Logger.Information("Mirror window lazy initialization completed successfully");
            return mirrorWindow;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create mirror window via lazy initialization");
            throw;
        }
    }

    private void NotificationManager_NotificationInvoked(AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        Logger.Information("Notification invoked with user input: {UserInput}", args.UserInput?.Count ?? 0);
        HandleNotification(args);
    }

    private void HandleNotification(AppNotificationActivatedEventArgs data)
    {
        Logger.Debug("Handling notification with arguments: {Arguments}", data.Arguments?.Count ?? 0);

        try
        {
            var dispatcherQueue = MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

            dispatcherQueue.TryEnqueue(async delegate
            {
                if (data.Arguments["action"] == "OpenApp")
                {
                    Logger.Information("Opening main window from notification");
                    if (MainWindow == null)
                    {
                        SetupMainWindow();
                    }
                }
                else if (data.Arguments["action"] == "ToastClick")
                {
                    Logger.Information("Notification toast clicked, showing mirror window");
                    MirrorWindow.Show();
                }
                Logger.Verbose("Executing notification handler on UI thread");
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

            Logger.Information("Main window setup completed successfully");
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
            return;
        }

        Logger.Debug("Main window visibility changed. Visible: {IsVisible}", args.Visible);

        try
        {
            if (!args.Visible)
            {
                Logger.Information("Main window hidden, showing mirror window");
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
        Logger.Information("Main window closing event triggered");

        try
        {
            Logger.Debug("Saving application state before closing");
            GetService<IStoreService>().SaveNow().Wait();
            Logger.Information("Application state saved successfully");

            Logger.Debug("Setting application closing flag");
            Interlocked.Exchange(ref isClosing, true);

            Logger.Debug("Closing mirror window if initialized");
            if (_lazyMirrorWindow.IsValueCreated)
            {
                MirrorWindow.Close();
            }

            Logger.Information("Application shutdown sequence completed");

            // Close and flush Serilog
            Log.CloseAndFlush();
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
            var errorWindow = new ErrorWindow
            {
                ReportedException = e.Exception
            };
            errorWindow.Show();
            e.Handled = true;
            Logger.Information("Error window displayed for unhandled exception");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to display error window for unhandled exception");
        }
    }
}
