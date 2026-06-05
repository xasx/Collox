

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Collox;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ErrorWindow : Window
{
    public ErrorWindow(Exception exception)
    {
        // ReportedException must be set before InitializeComponent so the
        // {x:Bind ReportedException, Mode=OneTime} in ErrorWindow.xaml has
        // a value to capture at first evaluation.
        ReportedException = exception;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
    }

    public Exception ReportedException { get; set; }
}
