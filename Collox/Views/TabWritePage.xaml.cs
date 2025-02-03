using Microsoft.UI.Xaml.Input;


namespace Collox.Views;

public sealed partial class TabWritePage : Page
{
    public TabWritePage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<TabWriteViewModel>();
    }

    private TabWriteViewModel ViewModel => DataContext as TabWriteViewModel;


    private void TabViewItem_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
    {
        var item = args.Item as TabData;
        if (item != null)
        {
            ViewModel.Contexts.Remove(item);
        }
    }


    private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {

        int tabToSelect = 0;

        switch (sender.Key)
        {
            case Windows.System.VirtualKey.Number1:
                tabToSelect = 0;
                break;
            case Windows.System.VirtualKey.Number2:
                tabToSelect = 1;
                break;
            case Windows.System.VirtualKey.Number3:
                tabToSelect = 2;
                break;
            case Windows.System.VirtualKey.Number4:
                tabToSelect = 3;
                break;
            case Windows.System.VirtualKey.Number5:
                tabToSelect = 4;
                break;
            case Windows.System.VirtualKey.Number6:
                tabToSelect = 5;
                break;
            case Windows.System.VirtualKey.Number7:
                tabToSelect = 6;
                break;
            case Windows.System.VirtualKey.Number8:
                tabToSelect = 7;
                break;
            case Windows.System.VirtualKey.Number9:
                // Select the last tab
                tabToSelect = ViewModel.Contexts.Count - 1;
                break;
        }

        // Only select the tab if it is in the list
        if (tabToSelect < ViewModel.Contexts.Count)
        {
            ViewModel.SelectedTab = ViewModel.Contexts[tabToSelect];
        }

        args.Handled = true;
    }

    private void CloseCurrentTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.RemoveContextCommand.Execute(null);
        args.Handled = true;
    }

    private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.AddContextCommand.Execute(null);
        args.Handled = true;
    }
}
