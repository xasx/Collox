using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.Mvvm.Messaging;
using Cottle;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

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
        InitializeComponent();
        DataContext = App.GetService<WriteViewModel>();
        WeakReferenceMessenger.Default.Register<TextSubmittedMessage>(this, (s, e) =>
        {
            TbInput.Focus(FocusState.Programmatic);
            Scroller.ScrollTo(0.0, Scroller.ViewportHeight,
                new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
        });
        WeakReferenceMessenger.Default.Register<ParagraphSelectedMessage>(this, (s, e) =>
        {
            var item = irChat.TryGetElement(e.Value) as FrameworkElement;
            if (item != null)
            {
                // Translate the item’s position into the scroller’s coordinate space
                var transform = item.TransformToVisual(Scroller);
                var offset = transform.TransformPoint(new Point(0, 0));

                // Scroll to that position
                Scroller.ScrollTo(offset.X, offset.Y,
                    new ScrollingScrollOptions(ScrollingAnimationMode.Enabled));
            }
        });
    }

    public WriteViewModel ViewModel => (WriteViewModel)DataContext;

   
    public TabData ConversationContext
    {
        get;
        set {
            field = value;
            ViewModel.ConversationContext = value;
        }
    }

    private void InputBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.KeyStrokesCount++;
        if (e.Key == VirtualKey.Enter)
        {
            // todo does not always work
            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                == CoreVirtualKeyStates.Down ||
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock)
                == CoreVirtualKeyStates.Down))
            {
                e.Handled = false;
            }
            else
            {
                BtnSubmit.Command.Execute(null);
                //This will prevent system from adding new line
                e.Handled = true;
            }
        }
        else
        {
            e.Handled = false;
        }
    }

    private void InputBox_Loaded(object sender, RoutedEventArgs e)
    {
        TbInput.Focus(FocusState.Programmatic);
    }

    private const string predefined = "predefined";
    private async void TemplatesFlyout_Opening(object sender, object e)
    {
        var vm = App.GetService<TemplatesViewModel>();
        await vm.LoadTemplates();
        var gfi = TemplatesFlyout.Items
            .Where(item => (string)item.Tag != predefined).ToList();

        foreach (var item in gfi)
        {
            TemplatesFlyout.Items.Remove(item);
        }

        foreach (var templateItem in vm.Templates)
        {
            TemplatesFlyout.Items.Add(
                new MenuFlyoutItem
                {
                    Text = templateItem.Name,
                    Icon = new SymbolIcon(Symbol.Document),
                    Tag = templateItem.Content,
                    Command = new RelayCommand(() =>
                    {
                        var doc = Document.CreateDefault(templateItem.Content).DocumentOrThrow;
                        var tti = doc.Render(Context.CreateBuiltin(new Dictionary<Value, Value>
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
        TbInput.Focus(FocusState.Programmatic);
        TbInput.Select(TbInput.Text.Length, 0);
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveNowCommand.ExecuteAsync(null);
        var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
        navService.Navigate(typeof(TemplatesPage));
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Setfly.Hide();
    }

    private void ChangeModeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.SwitchModeCommand.Execute(null);
        args.Handled = true;
    }
}
