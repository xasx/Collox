using Windows.Storage.Pickers;

namespace Collox.ViewModels;
public partial class GeneralSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string BaseFolder { get; set; } = AppHelper.Settings.BaseFolder;

    [RelayCommand]
    public async Task SelectBaseFolder()
    {
        var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow,
            suggestedStartLocation: PickerLocationId.DocumentsLibrary);
        if (folder != null)
        {
            BaseFolder = folder.Path;
        }
    }

    partial void OnBaseFolderChanged(string value)
    {
        AppHelper.Settings.BaseFolder = value;
    }
}
