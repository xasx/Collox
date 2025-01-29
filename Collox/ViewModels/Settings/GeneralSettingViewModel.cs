using Windows.Storage.Pickers;

namespace Collox.ViewModels;
public partial class GeneralSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string BaseFolder { get; set; } = AppHelper.Settings.BaseFolder;

    [ObservableProperty]
    public partial TimeOnly RollOverTime { get; set; } = AppHelper.Settings.RollOverTime;

    [ObservableProperty]
    public partial bool CustomRotation { get; set; } = AppHelper.Settings.CustomRotation;

    [ObservableProperty]
    public partial bool WriteDelimiters { get; set; } = AppHelper.Settings.WriteDelimiters;

    [ObservableProperty]
    public partial bool DeferredWrite { get; set; } = AppHelper.Settings.DeferredWrite;

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

    partial void OnRollOverTimeChanged(TimeOnly value)
    {
        AppHelper.Settings.RollOverTime = value;
    }

    partial void OnCustomRotationChanged(bool value)
    {
        AppHelper.Settings.CustomRotation = value;
    }

    partial void OnWriteDelimitersChanged(bool value)
    {
        AppHelper.Settings.WriteDelimiters = value;
    }

    partial void OnDeferredWriteChanged(bool value)
    {
        AppHelper.Settings.DeferredWrite = value;
    }
}
