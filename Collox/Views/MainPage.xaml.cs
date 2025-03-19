using Windows.Win32;
using Microsoft.UI.Xaml.Controls.Primitives;
using AutoSuggestBoxHelper = DevWinUI.AutoSuggestBoxHelper;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Microsoft.Win32.SafeHandles;

namespace Collox.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        DataContext = App.GetService<MainViewModel>();
        InitializeComponent();
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);

        if (App.GetService<IJsonNavigationService>() is JsonNavigationService navService)
        {
            navService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
                .ConfigureDefaultPage(typeof(TabWritePage))
                .ConfigureSettingsPage(typeof(SettingsPage))
                .ConfigureTitleBar(AppTitleBar)
                .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
        }
    }

    public MainViewModel ViewModel => DataContext as MainViewModel;

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MainWindow);
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
    }

    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = PInvoke.FindWindow("progman", null);
        PInvoke.SendMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
    }

    private void Shield_Click(object sender, RoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(sender as Shield);
    }

    private void PowerOffButton_Click(object sender, RoutedEventArgs e)
    {
        EnableShutdownPrivilege();
        PInvoke.InitiateSystemShutdown(null, null, 60, true, false);
        DisableShutdownPrivilege();
    }

    private void RebootButton_Click(object sender, RoutedEventArgs e)
    {
        EnableShutdownPrivilege();
        PInvoke.InitiateSystemShutdown(null, null, 60, true, true);
        DisableShutdownPrivilege();
    }

    private void AbortButton_Click(object sender, RoutedEventArgs e)
    {
        EnableShutdownPrivilege();
        PInvoke.AbortSystemShutdown(null);
        DisableShutdownPrivilege();
    }

    private unsafe void EnableShutdownPrivilege()
    {
        HANDLE tokenHandle = default;
        LUID luid;

        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess(),
            TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, &tokenHandle))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!PInvoke.LookupPrivilegeValue(null, PInvoke.SE_SHUTDOWN_NAME, out luid))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new VariableLengthInlineArray<LUID_AND_ATTRIBUTES>()
        };
        tp.Privileges[0].Luid = luid;
        tp.Privileges[0].Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED;

        uint rl = 0;
        if (!PInvoke.AdjustTokenPrivileges(tokenHandle, false, &tp, 0, null, &rl))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        PInvoke.CloseHandle(tokenHandle);
    }

    private unsafe void DisableShutdownPrivilege()
    {
        HANDLE tokenHandle = default;
        LUID luid;
        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess(),
            TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, &tokenHandle))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        if (!PInvoke.LookupPrivilegeValue(null, PInvoke.SE_SHUTDOWN_NAME, out luid))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new VariableLengthInlineArray<LUID_AND_ATTRIBUTES>()
        };
        tp.Privileges[0].Luid = luid;
        tp.Privileges[0].Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_REMOVED;
        uint rl = 0;
        if (!PInvoke.AdjustTokenPrivileges(tokenHandle, false, &tp, 0, null, &rl))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        PInvoke.CloseHandle(tokenHandle);
    }
}
