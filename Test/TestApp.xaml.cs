namespace Test;

public partial class TestApp : Application
{
    public new static TestApp Current => (TestApp)Application.Current;
    public static Window MainWindow = Window.Current;
    public static IntPtr Hwnd => WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);

    public TestApp()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

        MainWindow = new MainWindow();

        MainWindow.Title = MainWindow.AppWindow.Title = "Test";
        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
        MainWindow.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        MainWindow.Activate();

        UITestMethodAttribute.DispatcherQueue = MainWindow.DispatcherQueue;
        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
    }
}

