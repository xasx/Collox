namespace Collox.Views;

public sealed partial class GeneralSettingPage : Page
{
    public GeneralSettingViewModel ViewModel => (GeneralSettingViewModel)this.DataContext;
    public GeneralSettingPage()
    {
        this.DataContext = App.GetService<GeneralSettingViewModel>();
        this.InitializeComponent();
    }
}


