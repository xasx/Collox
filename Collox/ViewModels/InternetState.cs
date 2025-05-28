namespace Collox.ViewModels;

public partial class InternetState : ObservableObject
{
    [ObservableProperty] public partial string Icon { get; set; } = "\uF384";

    [ObservableProperty] public partial string State { get; set; } = "offline";
}
