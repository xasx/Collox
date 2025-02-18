namespace Collox.Views;

public sealed partial class AboutUsSettingPage : Page
{
    public AboutUsSettingPage()
    {
        ViewModel = App.GetService<AboutUsSettingViewModel>();
        InitializeComponent();
    }

    public AboutUsSettingViewModel ViewModel { get; }
}
