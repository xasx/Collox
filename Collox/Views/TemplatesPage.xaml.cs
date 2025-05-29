using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TemplatesPage : Page
{
    public TemplatesPage()
    {
        DataContext = App.GetService<TemplatesViewModel>();
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<TemplateEditedMessage>(this, (r, m) =>
        {
            tbName.SelectAll();
            tbName.Focus(FocusState.Programmatic);
        });
    }

    private TemplatesViewModel ViewModel => DataContext as TemplatesViewModel;

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var options = new FlyoutShowOptions
        {
            Placement = FlyoutPlacementMode.Right,
            ShowMode = FlyoutShowMode.Standard
        };
        var gv = e.OriginalSource as GridView;
        var cc = gv.ContainerFromItem(e.ClickedItem);
        //ComBarFly.ShowAt(cc, options);
    }
}
