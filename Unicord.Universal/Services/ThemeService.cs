using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Unicord.Universal.Services
{
    public enum AppTheme
    {
        OLED,
        Fluent,
        Performance,
        SunValley
    }

    internal class ThemeService : BaseService<ThemeService>
    {
        public AppTheme GetTheme()
        {
#if true
            return AppTheme.Fluent;
            return GetDefaultAppTheme();
#else
            if (App.LocalSettings.TryRead<int>("AppTheme", out var theme))
                return (AppTheme)theme;

            var defaultTheme = GetDefaultAppTheme();
            App.LocalSettings.Save("AppTheme", (int)defaultTheme);

            return defaultTheme;
#endif
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
