using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        DataContext = App.GetService<HistoryViewModel>();
        InitializeComponent();

        foreach (var item in ViewModel.Histories)
        {
            Debug.WriteLine(item.GetType());
        }
    }

    public HistoryViewModel ViewModel => DataContext as HistoryViewModel;
}
