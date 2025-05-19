using System.Diagnostics;
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Collox.Views;

public sealed partial class TabWritePage : Page
{
    public TabWritePage()
    {
        DataContext = App.GetService<TabWriteViewModel>();
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<FocusTabMessage>(this, (__, e) =>
        {
            var tim = DispatcherQueue.CreateTimer();
            tim.Interval = TimeSpan.FromMilliseconds(100);

            tim.Tick += (_, ___) =>
            {
                SetFocusOnTab(e.Value, MainTabView);
                tim.Stop();
            };
            tim.Start();
        });

        WeakReferenceMessenger.Default.Register<TabWritePage, GetFrameRequestMessage>(this, (s, e) => e.Reply(FindTabFrame(MainTabView)));
    }

    private TabWriteViewModel ViewModel => DataContext as TabWriteViewModel;

    private void SetFocusOnTab(TabData tab, DependencyObject root)
    {
        var c = VisualTreeHelper.GetChildrenCount(root);

        for (var i = 0; i < c; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBox tb)
            {
                if (tb.Tag is TabData td)
                {
                    if (td == tab)
                    {
                        tb.SelectAll();
                        tb.Focus(FocusState.Programmatic);
                        return;
                    }
                }
            }
            else
            {
                if (child is WritePage)
                {
                    continue;
                }

                Debug.WriteLine(child);
                SetFocusOnTab(tab, child);
            }
        }
    }
    private Frame FindTabFrame(DependencyObject root)
    {
        var c = VisualTreeHelper.GetChildrenCount(root);

        for (var i = 0; i < c; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Frame frame)
            {
                return frame;
            }
            else
            {
                if (child is WritePage)
                {
                    continue;
                }

                var childFrame = FindTabFrame(child);
                if (childFrame is not null ) {
                    return childFrame;
                }
            }
        }
        return null;
    }

    private void TabViewItem_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is TabData item)
        {
            ViewModel.RemoveTab(item);
        }
    }

    private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        var tabToSelect = 0;

        switch (sender.Key)
        {
            case VirtualKey.Number1:
                tabToSelect = 0;
                break;
            case VirtualKey.Number2:
                tabToSelect = 1;
                break;
            case VirtualKey.Number3:
                tabToSelect = 2;
                break;
            case VirtualKey.Number4:
                tabToSelect = 3;
                break;
            case VirtualKey.Number5:
                tabToSelect = 4;
                break;
            case VirtualKey.Number6:
                tabToSelect = 5;
                break;
            case VirtualKey.Number7:
                tabToSelect = 6;
                break;
            case VirtualKey.Number8:
                tabToSelect = 7;
                break;
            case VirtualKey.Number9:
                // Select the last tab
                tabToSelect = ViewModel.Tabs.Count - 1;
                break;
        }

        // Only select the tab if it is in the list
        if (tabToSelect < ViewModel.Tabs.Count)
        {
            ViewModel.SelectedTab = ViewModel.Tabs[tabToSelect];
        }

        args.Handled = true;
    }

    private void CloseCurrentTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.CloseSelectedTabCommand.Execute(null);
        args.Handled = true;
    }

    private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.AddNewTabCommand.Execute(null);
        args.Handled = true;
    }

    private void ContextBox_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var tb = sender as TextBox;
            if (tb?.Tag is TabData td)
            {
                td.IsEditing = false;
                ViewModel.UpdateContext(td);
            }

            e.Handled = true;
        }
    }

    private void MainTabView_AddTabButtonClick(TabView sender, object args)
    {
        ViewModel.AddNewTabCommand.Execute(null);
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        // SetFocusOnTab(ViewModel.SelectedTab, MainTabView);
        App.MirrorWindow.Activate();
    }
}
