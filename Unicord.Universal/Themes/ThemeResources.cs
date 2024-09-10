using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Services;
using Windows.UI.Xaml;

namespace Unicord.Universal.Themes
{
    internal class ThemeResources : ResourceDictionary
    {
        public ThemeResources()
        {
            var theme = ThemeService.GetForCurrentView()
                .GetTheme();

            switch (theme)
            {
                case AppTheme.OLED:
                    this.Source = new Uri("ms-appx:///Themes/Styles/OLED.xaml");
                    break;
                case AppTheme.Fluent:
                    this.Source = new Uri("ms-appx:///Themes/Styles/Fluent.xaml");
                    break;
                case AppTheme.Performance:
                    this.Source = new Uri("ms-appx:///Themes/Styles/Performance.xaml");
                    break;
                case AppTheme.SunValley:
                    this.Source = new Uri("ms-appx:///Themes/Styles/SunValley.xaml");
                    break;
            }

        }
    }
}
