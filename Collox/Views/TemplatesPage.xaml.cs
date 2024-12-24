using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TemplatesPage : Page
{
    public TemplatesPage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<TemplatesViewModel>();
    }

    TemplatesViewModel ViewModel => (TemplatesViewModel)this.DataContext;

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        FlyoutShowOptions options = new FlyoutShowOptions();
        options.Placement = FlyoutPlacementMode.Right;
        options.ShowMode = FlyoutShowMode.Standard;
        var gv = e.OriginalSource as GridView;
        var cc = gv.ContainerFromItem(e.ClickedItem);
        ComBarFly.ShowAt(cc, options);
    }

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        tbName.SelectAll();
        tbName.Focus(FocusState.Programmatic);
    }

    private void Page_Loading(FrameworkElement sender, object args)
    {
        ViewModel.LoadTemplatesCommand.Execute(sender);
    }
}
