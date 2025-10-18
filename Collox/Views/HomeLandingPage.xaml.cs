namespace Collox.Views;

public sealed partial class HomeLandingPage : Page
{
    public HomeLandingPage()
    {
        //DataContext = App.GetService<HomeLandingViewModel>();
        InitializeComponent();
    }

    // public HomeLandingViewModel ViewModel => DataContext as HomeLandingViewModel;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageLoadStoryboard.Begin();
    }
}
