using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WritePage : Page
{
    public WritePage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<WriteViewModel>();
        WeakReferenceMessenger.Default.Register<TextSubmittedMessage>(this, (s, e) =>
        {
            tbInput.Focus(FocusState.Programmatic);
            scroller.ChangeView(null, scroller.ScrollableHeight, null);
        });
    }
    
    public WriteViewModel ViewModel => (WriteViewModel)this.DataContext;

    private void tbInput_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            == CoreVirtualKeyStates.Down ||
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock)
            == CoreVirtualKeyStates.Down) && e.Key == VirtualKey.Enter)
        {
            //ViewModel.LastParagraph += Environment.NewLine;
            //tbInput.Text += Environment.NewLine;
            //tbInput.Select(tbInput.Text.Length, 0);
            e.Handled = false;
            
        }
        else if (e.Key == VirtualKey.Enter)
        {
            btnSubmit.Command.Execute(null);
            //This will prevent system from adding new line
            e.Handled = true;
        }
        else
        {
            e.Handled = false;
        }
    }
}
