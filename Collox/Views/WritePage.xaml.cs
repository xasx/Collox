using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Cottle;
using EmojiToolkit;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Collox.Views;

public sealed partial class WritePage : Page
{
    private const string predefined = "predefined";
    private ScrollViewer _messageScrollViewer;

    public WritePage()
    {
        DataContext = App.GetService<WriteViewModel>();
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<TextSubmittedMessage>(this,
            (s, e) => InputTextBox.Focus(FocusState.Programmatic));

        WeakReferenceMessenger.Default.Register<MessageSelectedMessage>(this,
            (s, e) => MessageListView.ScrollIntoView(e.Value));

        // Add this line to get the ScrollViewer after the control is loaded
        Loaded += WritePage_Loaded;
    }

    private void WritePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Get the ScrollViewer from the ListView
        _messageScrollViewer = FindChildOfType<ScrollViewer>(MessageListView);
        if (_messageScrollViewer != null)
        {
            _messageScrollViewer.ViewChanged += MessageScrollViewer_ViewChanged;
        }
    }

    private void MessageScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_messageScrollViewer == null || MessageListView.Items.Count == 0)
        {
            ScrollPopup.IsOpen = false;
            return;
        }

        // Check if scrolled to bottom
        var isAtBottom = _messageScrollViewer.VerticalOffset >=
                        _messageScrollViewer.ScrollableHeight - 50; // 50px threshold

        // Show popup only when not at bottom
        ScrollPopup.IsOpen = !isAtBottom;
    }

    private static T FindChildOfType<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        if (root is T typed) return typed;

        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindChildOfType<T>(child);
            if (result != null) return result;
        }

        return null;
    }

    public WriteViewModel ViewModel => DataContext as WriteViewModel;

    public TabData ConversationContext
    {
        get;
        set
        {
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
            if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                == CoreVirtualKeyStates.Down ||
                InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock)
                == CoreVirtualKeyStates.Down)
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
        InputTextBox.Focus(FocusState.Programmatic);
    }

    private async void TemplatesFlyout_Opening(object sender, object e)
    {
        try
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
                        Command = new RelayCommand(() => ApplyTemplate(templateItem))
                    });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in TemplatesFlyout_Opening: {ex.Message}");
            // Optionally, handle the exception (e.g., show a message to the user)
        }
    }

    private void ApplyTemplate(Template templateItem)
    {
        var doc = Document.CreateDefault(templateItem.Content).DocumentOrThrow;

        var cc = Context.CreateCustom((value) =>
        {
            return value.AsString switch
            {
                "now" => Value.FromString(DateTime.Now.ToString("F")),
                "random_emoji_nature" => Value.FromString(
                    Emoji.All.Where(e => e.Category == "nature")
                        .OrderBy(e => Random.Shared.Next()).First().Raw),
                _ => Value.Undefined
            };
        });
        var tti = doc.Render(Context.CreateBuiltin(cc));
        ViewModel.InputMessage += tti;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as Button;
        ViewModel.InputMessage += b!.Tag;
        ViewModel.KeyStrokesCount++;
        InputTextBox.Focus(FocusState.Programmatic);
        InputTextBox.Select(InputTextBox.Text.Length, 0);
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.SaveNowCommand.ExecuteAsync(null);
            var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
            Debug.Assert(navService != null, nameof(navService) + " != null");
            navService.Navigate(typeof(TemplatesPage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in MenuFlyoutItem_Click: {ex.Message}");
            // Optionally, handle the exception (e.g., show a message to the user)
        }
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        VoiceSettingsFlyout.Hide();
    }

    private void ChangeModeKeyboardAccelerator_Invoked(KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.SwitchModeCommand.Execute(null);
        args.Handled = true;
    }

    private void ProcessorFlyout_Opening(object sender, object e)
    {
        var aiSettings = App.GetService<AISettingsViewModel>();
        ViewModel.AvailableProcessors.Clear();
        var actives = ViewModel.ConversationContext.ActiveProcessors.Select(p => p.Id);
        var selection = new List<IntelligentProcessorViewModel>();
        foreach (var processor in aiSettings.Enhancers)
        {
            ViewModel.AvailableProcessors.Add(processor);
            if (actives.Contains(processor.Id))
            {
                selection.Add(processor);
            }
        }

        ProcessorsListView.SelectedItems.AddRange(selection);
    }

    private void ProcessorFlyout_Closed(object sender, object e)
    {
        ViewModel.ConversationContext.ActiveProcessors.Clear();
        ViewModel.ConversationContext.ActiveProcessors.AddRange(
            [.. ProcessorsListView.SelectedItems.Cast<IntelligentProcessorViewModel>().Select(p => p.Model)]);
        WeakReferenceMessenger.Default.Send(new UpdateTabMessage(ViewModel.ConversationContext));
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessorFlyout.Hide();
    }

    private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
    {
        _messageScrollViewer?.ChangeView(null, _messageScrollViewer.ScrollableHeight, null, true);
        ScrollPopup.IsOpen = false;
    }
}
