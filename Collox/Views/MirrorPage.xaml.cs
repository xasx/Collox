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
using Windows.Graphics.Display;
using WinUIEx;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MirrorPage : Page
    {
        public MirrorViewModel ViewModel => DataContext as MirrorViewModel;

        public MirrorPage()
        {
            this.InitializeComponent();
            DataContext = App.GetService<MirrorViewModel>();

            App.MirrorWindow.ExtendsContentIntoTitleBar = true;
            // App.MirrorWindow.SetTitleBar(TitleBar);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        
    }
}
