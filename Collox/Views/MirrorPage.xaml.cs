using Microsoft.UI.Windowing;
using WinUIEx;

namespace Collox.Views;

public sealed partial class MirrorPage : Page
{
    public MirrorViewModel ViewModel => DataContext as MirrorViewModel;

    public MirrorPage()
    {
        this.InitializeComponent();
        DataContext = App.GetService<MirrorViewModel>();

        App.MirrorWindow.ExtendsContentIntoTitleBar = true;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var scale = XamlRoot.RasterizationScale;
        var posX = DisplayArea.Primary.WorkArea.Width - 640 * scale;
        App.MirrorWindow.MoveAndResize((int)posX, 0, 640, 400);
        App.MirrorWindow.SetForegroundWindow();
        App.MirrorWindow.Show();
    }

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MirrorWindow);
    }
}
