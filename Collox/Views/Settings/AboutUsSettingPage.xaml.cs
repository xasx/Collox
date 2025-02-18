namespace Collox.Views;

public sealed partial class AboutUsSettingPage : Page
{
    public AboutUsSettingPage()
    {
        DataContext = App.GetService<AboutUsSettingViewModel>();
        InitializeComponent();
    }

    public AboutUsSettingViewModel ViewModel => DataContext as AboutUsSettingViewModel;
}
