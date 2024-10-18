using System;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Unicord.Universal.Themes
{
    internal class ThemeResources : ResourceDictionary
    {
        public ThemeResources()
        {
            var theme = ThemeService.GetForCurrentView()
                .GetTheme();

            Uri uri = theme switch
            {
                AppTheme.OLED => new Uri("ms-appx:///Themes/Styles/OLED.xaml"),
                AppTheme.Fluent => new Uri("ms-appx:///Themes/Styles/Fluent.xaml"),
                AppTheme.Performance => new Uri("ms-appx:///Themes/Styles/Performance.xaml"),
                AppTheme.SunValley => new Uri("ms-appx:///Themes/Styles/SunValley.xaml"),
                _ => throw new InvalidOperationException("Unknown theme"),
            };

            Application.LoadComponent(this, uri, ComponentResourceLocation.Application);
        }
    }
}
