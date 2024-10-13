using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Unicord.Universal.Services
{
    public enum AppTheme
    {
        Performance,
        Fluent,
        SunValley,
        OLED
    }

    internal class ThemeService : BaseService<ThemeService>
    {
        public AppTheme GetTheme()
        {
            if (App.LocalSettings.TryRead<int>("AppThemeSet", out var value))
            {
                App.LocalSettings.Save("AppTheme", value);
                App.LocalSettings.Delete("AppThemeSet");
            }

            if (App.LocalSettings.TryRead<int>("AppTheme", out var theme))
                return (AppTheme)theme;

            var defaultTheme = GetDefaultAppTheme();
            App.LocalSettings.Save("AppTheme", (int)defaultTheme);

            return defaultTheme;
        }

        public AppTheme GetSettingsTheme()
        {
            if (App.LocalSettings.TryRead<int>("AppThemeSet", out var value))
            {
                return (AppTheme)value;
            }

            if (App.LocalSettings.TryRead<int>("AppTheme", out var theme))
                return (AppTheme)theme;

            return GetDefaultAppTheme();
        }

        public void SetThemeOnRelaunch(AppTheme theme)
        {
            App.LocalSettings.Save("AppThemeSet", (int)theme);
        }

        public AppTheme GetDefaultAppTheme()
        {
            var osBuild = SystemInformation.Instance.OperatingSystemVersion.Build;
            if (osBuild >= 22000)
                return AppTheme.SunValley;

            if (SystemInformation.Instance.DeviceFamily == "Windows.Mobile")
                return AppTheme.Performance;

            return AppTheme.Fluent;
        }
    }
}
