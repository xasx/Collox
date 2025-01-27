using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Speech.Recognition;
using ABI.Windows.UI.Text;
using CommunityToolkit.Mvvm.Messaging;
using Cottle;
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
            scroller.ScrollTo(0.0, scroller.ViewportHeight,
                new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
        });
        WeakReferenceMessenger.Default.Register<ParagraphSelectedMessage>(this, (s, e) =>
        {
            var item = irChat.TryGetElement(e.Value) as FrameworkElement;
            if (item != null)
            {
                // Translate the item’s position into the scroller’s coordinate space
                var transform = item.TransformToVisual(scroller);
                Point offset = transform.TransformPoint(new Point(0, 0));

                // Scroll to that position
                scroller.ScrollTo(offset.X, offset.Y,
                    new ScrollingScrollOptions(ScrollingAnimationMode.Enabled));
            }
        });
    }

    public WriteViewModel ViewModel => (WriteViewModel)this.DataContext;

    private void tbInput_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.KeyStrokesCount++;
        if (e.Key == VirtualKey.Enter)
        {
            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                == CoreVirtualKeyStates.Down ||
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock)
                == CoreVirtualKeyStates.Down))
            {
                //ViewModel.LastParagraph += Environment.NewLine;
                //tbInput.Text += Environment.NewLine;
                //tbInput.Select(tbInput.Text.Length, 0);
                e.Handled = false;
            }
            else
            {
                // todo test for empty input and do smth
                btnSubmit.Command.Execute(null);
                //This will prevent system from adding new line
                e.Handled = true;
            }
        }
        else

        {
            e.Handled = false;
        }
    }

    private void tbInput_Loaded(object sender, RoutedEventArgs e)
    {
        tbInput.Focus(FocusState.Programmatic);
    }

    private const string predefined = "predefined";
    private async void templatesFlyout_Opening(object sender, object e)
    {
        // todo no abuse
        TemplatesViewModel vm = App.GetService<TemplatesViewModel>();
        await vm.LoadTemplates();
        var gfi = templatesFlyout.Items
            .Where((item) => (string)item.Tag != predefined).ToList();
        //var gfi =  from item in templatesFlyout.Items
        //           where item.Tag == generated
        //           select item;


        foreach (var item in gfi)
        {
            templatesFlyout.Items.Remove(item);
        }

        foreach (var tt in vm.Templates)
        {
            templatesFlyout.Items.Add(
                new MenuFlyoutItem()
                {
                    Text = tt.Name,
                    Icon = new SymbolIcon(Symbol.Document),
                    Tag = tt.Content,
                    Command = new RelayCommand(() =>
                    {
                        var doc = Document.CreateDefault(tt.Content).DocumentOrThrow;
                        var tti = doc.Render(Context.CreateBuiltin(new Dictionary<Value, Value>()
                        {
                            ["now"] = Value.FromLazy(() => Value.FromString(DateTime.Now.ToString("F")))
                        }));
                        ViewModel.LastParagraph += tti;
                    })
                });
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as Button;
        ViewModel.LastParagraph += b.Tag;
        ViewModel.KeyStrokesCount++;
        tbInput.Focus(FocusState.Programmatic);
        tbInput.Select(tbInput.Text.Length, 0);
        //svEmo.ScrollTo()
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveNowCommand.ExecuteAsync(null);
        var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
        navService.Navigate(typeof(TemplatesPage));
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        setfly.Hide();
    }
}
