using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Win32;

namespace Collox.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);

        var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
        if (navService != null)
        {
            navService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
.ConfigureDefaultPage(typeof(WritePage))
.ConfigureSettingsPage(typeof(SettingsPage))
                .ConfigureTitleBar(AppTitleBar)
                .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
        }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MainWindow);
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        DevWinUI.AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        DevWinUI.AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
    }

    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = PInvoke.FindWindow("progman", null);
        PInvoke.SendMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
    }

    private void Shield_Click(object sender, RoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(sender as Shield);
    }
}

