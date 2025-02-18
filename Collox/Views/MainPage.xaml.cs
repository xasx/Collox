using Windows.Win32;
using Microsoft.UI.Xaml.Controls.Primitives;
using AutoSuggestBoxHelper = DevWinUI.AutoSuggestBoxHelper;

namespace Collox.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        DataContext = App.GetService<MainViewModel>();
        InitializeComponent();
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);

        if (App.GetService<IJsonNavigationService>() is JsonNavigationService navService)
        {
            navService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
                .ConfigureDefaultPage(typeof(TabWritePage))
                .ConfigureSettingsPage(typeof(SettingsPage))
                .ConfigureTitleBar(AppTitleBar)
                .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
        }
    }

    public MainViewModel ViewModel => DataContext as MainViewModel;

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MainWindow);
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
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
