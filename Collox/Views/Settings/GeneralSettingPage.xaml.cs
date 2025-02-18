namespace Collox.Views;

public sealed partial class GeneralSettingPage : Page
{
    public GeneralSettingPage()
    {
        DataContext = App.GetService<GeneralSettingViewModel>();
        InitializeComponent();
    }

    public GeneralSettingViewModel ViewModel => (GeneralSettingViewModel)DataContext;
}
