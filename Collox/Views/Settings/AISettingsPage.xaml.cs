namespace Collox.Views;

public sealed partial class AISettingPage : Page
{
    public AISettingPage()
    {
        DataContext = App.GetService<AISettingsViewModel>();
        InitializeComponent();
    }

    public AISettingsViewModel ViewModel => DataContext as AISettingsViewModel;
}
