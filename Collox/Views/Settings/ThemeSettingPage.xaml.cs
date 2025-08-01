using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Media;

namespace Collox.Views;

public sealed partial class ThemeSettingPage : Page
{
    public ThemeSettingPage()
    {
        InitializeComponent();
    }

    private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        TintBox.Fill = new SolidColorBrush(args.NewColor);
        App.Current.GetThemeService.SetBackdropTintColor(args.NewColor);
    }

    private void ColorPalette_ColorChanged(object sender, ColorPaletteColorChangedEventArgs e)
    {
        if (e.Color.Equals(Colors.Black) || e.Color.Equals(Colors.Transparent))
        {
            App.Current.GetThemeService.ResetBackdropProperties();
        }
        else
        {
            App.Current.GetThemeService.SetBackdropTintColor(e.Color);
        }

        TintBox.Fill = new SolidColorBrush(e.Color);
    }
}
