using System.Diagnostics;
using Collox.Services;
using Windows.Win32;

namespace Collox;

public partial class App : Application
{
    public static Window MainWindow = Window.Current;
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
        this.InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
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

        services.AddSingleton<IStoreService, StoreService>();
        services.AddSingleton<ITemplateService, TemplateService>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();

        if (MainWindow.Content is not Frame rootFrame)
        {
            MainWindow.Content = rootFrame = new Frame();
        }

        if (GetThemeService != null)
        {
            GetThemeService.AutoInitialize(MainWindow);
        }

        rootFrame.Navigate(typeof(MainPage));

        MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

        MainWindow.Activate();

        // right place?
        this.UnhandledException += Application_UnhandledException;
    }

    private void Application_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"An error {e.Exception.Message}{Environment.NewLine}{e}");
        MessageBox.Show(WinRT.Interop.WindowNative.GetWindowHandle(MainWindow),
            $"{e.Exception.Message}{Environment.NewLine}{e}", "Error", MessageBoxStyle.ApplicationModal | MessageBoxStyle.IconError | MessageBoxStyle.Ok);
    }
}

