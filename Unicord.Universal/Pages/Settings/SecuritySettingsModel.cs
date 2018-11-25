using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Pages.Settings
{
    class SecuritySettingsModel
    {
        public bool HelloForNSFW
        {
            get => App.RoamingSettings.Read("HelloForNSFW", false);
            set => App.RoamingSettings.Save("HelloForNSFW", value);
        }

        public bool HelloForSettings
        {
            get => App.RoamingSettings.Read("HelloForSettings", false);
            set => App.RoamingSettings.Save("HelloForSettings", value);
        }

        public TimeSpan AuthenticationTime
        {
            get => App.RoamingSettings.Read("AuthenticationTime", TimeSpan.FromMinutes(5));
            set => App.RoamingSettings.Save("AuthenticationTime", value);
        }
    }
}
