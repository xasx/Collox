using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using WinUIEx;

namespace Collox.Views;

public sealed partial class MirrorPage : Page
{
    public MirrorViewModel ViewModel => DataContext as MirrorViewModel;

    public MirrorPage()
    {
        InitializeComponent();
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

        App.MirrorWindow.SetDragMove(this);
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MirrorWindow);
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        App.MirrorWindow.Hide();
    }

    private void TokenView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedContexts = new ObservableCollection<string>(TokenView.SelectedItems.Cast<string>());
    }
}
