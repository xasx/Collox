namespace Collox.ViewModels;

public partial class InternetState : ObservableObject
{
    [ObservableProperty] public partial bool IsConnected { get; set; } = false;
}
