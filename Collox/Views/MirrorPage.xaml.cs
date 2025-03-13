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
}
