namespace Collox.Views;

public sealed partial class AppUpdateSettingPage : Page
{
    public AppUpdateSettingPage()
    {
        DataContext = App.GetService<AppUpdateSettingViewModel>();
        InitializeComponent();
    }

    public AppUpdateSettingViewModel ViewModel => DataContext as AppUpdateSettingViewModel;
}
