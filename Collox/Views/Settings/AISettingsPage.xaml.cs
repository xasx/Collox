namespace Collox.Views;

public sealed partial class AISettingPage : Page
{
    public AISettingPage()
    {
        DataContext = App.GetService<AISettingsViewModel>();
        InitializeComponent();
    }

    public AISettingsViewModel ViewModel => DataContext as AISettingsViewModel;

    private void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // check whether Enter is pressed
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var tb = sender as Microsoft.UI.Xaml.Controls.TextBox;
            var proc = tb.Tag as IntelligentProcessorViewModel;
            proc.NamePresentation = "Display";
        }
    }

    private void TextBox_KeyDown_1(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var tb = sender as Microsoft.UI.Xaml.Controls.TextBox;
            var proc = tb.Tag as IntelligenceApiProviderViewModel;
            proc.NamePresentation = "Display";
        }
    }
}
