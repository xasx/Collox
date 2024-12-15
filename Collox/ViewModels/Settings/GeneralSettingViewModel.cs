namespace Collox.ViewModels;
public partial class GeneralSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string BaseFolder { get; set; } =
        AppHelper.Settings.BaseFolder;
}
