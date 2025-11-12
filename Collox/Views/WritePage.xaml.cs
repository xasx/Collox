using System.Diagnostics;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Controls;
using Cottle;
using EmojiToolkit;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace Collox.Views;

public sealed partial class WritePage : Page, IRecipient<TextSubmittedMessage>, IRecipient<MessageSelectedMessage>,
    IRecipient<FocusInputMessage>
{
    private const string predefined = "predefined";
    private ScrollViewer _messageScrollViewer;
    
    // Cache the emoji category map as a static readonly field
    private static readonly string[] _emojiCategoryMap = Emoji.All
        .Select(e => e.Category)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    
    private readonly Lazy<IContext> _context = new(() => Context.CreateBuiltin(Context.CreateCustom(
            (value) =>
            {
                return value.AsString switch
                {
                    "now" => Value.FromString(DateTime.Now.ToString("F")),
                    "random_emoji_nature" => Value.FromString(
                        Emoji.All.Where(e => e.Category == "nature").OrderBy(e => Random.Shared.Next()).First().Raw),
                    _ => Value.Undefined
                };
            })));

    public WritePage()
    {
        DataContext = App.GetService<WriteViewModel>();
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);

        // Add this line to get the ScrollViewer after the control is loaded
        Loaded += WritePage_Loaded;
        Unloaded += WritePage_Unloaded;
    }

    private void WritePage_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        // Clean up event handlers
        if (_messageScrollViewer != null)
        {
            _messageScrollViewer.ViewChanged -= MessageScrollViewer_ViewChanged;
        }
    }

    private void FocusInputBox()
    {
        InputTextBox.Focus(FocusState.Programmatic);
        InputTextBox.Select(InputTextBox.Text.Length, 0);
    }

    private void WritePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Re-register messenger if needed
        if (!WeakReferenceMessenger.Default.IsRegistered<TextSubmittedMessage>(this))
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

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
        var isAtBottom = _messageScrollViewer.VerticalOffset >= _messageScrollViewer.ScrollableHeight - 50; // 50px threshold

        // Show popup only when not at bottom
        ScrollPopup.IsOpen = !isAtBottom;
    }

    private static T FindChildOfType<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
            return null;
        if (root is T typed)
            return typed;

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindChildOfType<T>(child);
            if (result != null)
                return result;
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
        ViewModel.UpdateHitPercentage();

        if (e.Key == VirtualKey.Enter)
        {
            if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ||
                InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock)
                    .HasFlag(CoreVirtualKeyStates.Down))
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

    private void InputBox_Loaded(object sender, RoutedEventArgs e) => InputTextBox.Focus(FocusState.Programmatic);

    private async void TemplatesFlyout_Opening(object sender, object e)
    {
        try
        {
            var vm = App.GetService<TemplatesViewModel>();
            await vm.LoadTemplates();
            var gfi = TemplatesFlyout.Items.Where(item => (string)item.Tag != predefined).ToList();

            foreach (var item in gfi)
            {
                TemplatesFlyout.Items.Remove(item);
            }

            foreach (var templateItem in vm.Templates)
            {
                TemplatesFlyout.Items
                    .Add(
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
        var tti = doc.Render(_context.Value);
        
        // Get current cursor position
        var cursorPosition = InputTextBox.SelectionStart;
        
        // Insert template text at cursor position
        var currentText = ViewModel.InputMessage ?? string.Empty;
        ViewModel.InputMessage = currentText.Insert(cursorPosition, tti);
        ViewModel.KeyStrokesCount ++;

        // Move cursor to end of inserted text
        InputTextBox.SelectionStart = cursorPosition + tti.Length;
        InputTextBox.SelectionLength = 0;
        
        // Focus the text box
        InputTextBox.Focus(FocusState.Programmatic);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var b = sender as Button;
        
        // Get current cursor position
        var cursorPosition = InputTextBox.SelectionStart;
        
        // Insert button tag text at cursor position
        var currentText = ViewModel.InputMessage ?? string.Empty;
        var tagText = b!.Tag?.ToString() ?? string.Empty;
        ViewModel.InputMessage = currentText.Insert(cursorPosition, tagText);
        
        ViewModel.KeyStrokesCount++;
        
        // Move cursor to end of inserted text
        InputTextBox.Focus(FocusState.Programmatic);
        InputTextBox.SelectionStart = cursorPosition + tagText.Length;
        InputTextBox.SelectionLength = 0;
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e) { VoiceSettingsFlyout.Hide(); }

    private void ChangeModeKeyboardAccelerator_Invoked(
        KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.SwitchModeCommand.Execute(null);
        args.Handled = true;
    }

    private async void ProcessorFlyout_Opening(object sender, object e)
    {
        try
        {
            // Show progress indicator
            InitializationProgressBar.Visibility = Visibility.Visible;
            InitializationProgressBar.IsIndeterminate = true;

            var aiSettings = App.GetService<AISettingsViewModel>();
            await aiSettings.InitializeAsync();

            ViewModel.AvailableProcessors.Clear();

            // Use HashSet for O(1) lookup instead of LINQ Contains
            var activesSet = new HashSet<Guid>(ViewModel.ConversationContext.ActiveProcessors.Select(p => p.Id));
            var selection = new List<IntelligentProcessorViewModel>();

            foreach (var processor in aiSettings.Processors)
            {
                ViewModel.AvailableProcessors.Add(processor);
                if (activesSet.Contains(processor.Id))
                {
                    selection.Add(processor);
                }
            }

            // Batch selection update
            ProcessorsListView.SelectedItems.Clear();
            foreach (var item in selection)
            {
                ProcessorsListView.SelectedItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ProcessorFlyout_Opening: {ex.Message}");
        }
        finally
        {
            // Hide progress indicator
            InitializationProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ProcessorFlyout_Closed(object sender, object e)
    {
        ViewModel.ConversationContext.ActiveProcessors.Clear();
        ViewModel.ConversationContext.ActiveProcessors
            .AddRange(ProcessorsListView.SelectedItems.Cast<IntelligentProcessorViewModel>().Select(p => p.Model));
        WeakReferenceMessenger.Default.Send(new UpdateTabMessage(ViewModel.ConversationContext));
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e) { ProcessorFlyout.Hide(); }

    private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
    {
        _messageScrollViewer?.ChangeView(null, _messageScrollViewer.ScrollableHeight, null, true);
        ScrollPopup.IsOpen = false;
    }

    private void ProcessorFlyout_Opened(object sender, object e)
    {

    }

    public void Receive(TextSubmittedMessage message) => InputTextBox.Focus(FocusState.Programmatic);

    public void Receive(MessageSelectedMessage message) => MessageListView.ScrollIntoView(message.Value);

    public void Receive(FocusInputMessage message) => FocusInputBox();

    private void EmojiSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not Segmented segmented || segmented.SelectedIndex == -1)
            return;

        // Map segment index to emoji category
        var selectedCategory = _emojiCategoryMap[segmented.SelectedIndex];
        
        // Find the ScrollView in the emoji flyout
        var scrollView = FindChildOfType<ScrollView>(EmojiFlyout.Content as FrameworkElement);
        if (scrollView == null)
            return;
        
        // Find the TextBlock with matching category in the ItemsRepeater
        var itemsRepeater = EmojiRepeater;
        if (itemsRepeater == null)
            return;
        
        // Find the group header TextBlock with the matching category
        for (var i = 0; i < itemsRepeater.ItemsSourceView.Count; i++)
        {
            var container = itemsRepeater.TryGetElement(i) as FrameworkElement;
            var headerTextBlock = FindChildOfType<TextBlock>(container);
            
            if (headerTextBlock?.Tag?.ToString()?.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) == true)
            {
                // Scroll the header into view
                headerTextBlock.StartBringIntoView(new BringIntoViewOptions
                {
                    AnimationDesired = true,
                    VerticalAlignmentRatio = 0.0 // Align to top
                });
                break;
            }
        }
    }
}
