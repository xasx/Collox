namespace Collox.Views;

public sealed partial class AppUpdateSettingPage : Page
{
    public AppUpdateSettingPage()
    {
        ViewModel = App.GetService<AppUpdateSettingViewModel>();
        InitializeComponent();
    }

    public AppUpdateSettingViewModel ViewModel { get; }
}
