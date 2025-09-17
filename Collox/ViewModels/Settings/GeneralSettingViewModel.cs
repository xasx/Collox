using Windows.Storage.Pickers;

namespace Collox.ViewModels;

public partial class GeneralSettingViewModel : ObservableObject
{
    [ObservableProperty] public partial string BaseFolder { get; set; } = Settings.BaseFolder;

    [ObservableProperty] public partial TimeOnly RollOverTime { get; set; } = Settings.RollOverTime;

    [ObservableProperty] public partial bool CustomRotation { get; set; } = Settings.CustomRotation;

    [ObservableProperty] public partial bool WriteDelimiters { get; set; } = Settings.WriteDelimiters;

    [ObservableProperty] public partial bool DeferredWrite { get; set; } = Settings.DeferredWrite;

    [ObservableProperty] public partial bool PersistMessages { get; set; } = Settings.PersistMessages;

    [RelayCommand]
    public async Task SelectBaseFolderAsync()
    {
        var picker = new FolderPicker()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        
        // Initialize the picker with the main window handle
        var mainWindow = App.MainWindow;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            BaseFolder = folder.Path;
        }
    }

    partial void OnBaseFolderChanged(string value)
    {
        Settings.BaseFolder = value;
    }

    partial void OnPersistMessagesChanged(bool value)
    {
        AppHelper.Settings.PersistMessages = value;
    }

    partial void OnRollOverTimeChanged(TimeOnly value)
    {
        Settings.RollOverTime = value;
    }

    partial void OnCustomRotationChanged(bool value)
    {
        Settings.CustomRotation = value;
    }

    partial void OnWriteDelimitersChanged(bool value)
    {
        Settings.WriteDelimiters = value;
    }

    partial void OnDeferredWriteChanged(bool value)
    {
        Settings.DeferredWrite = value;
    }
}
