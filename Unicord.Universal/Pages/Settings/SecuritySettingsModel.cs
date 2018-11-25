using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Unicord.Constants;

namespace Unicord.Universal.Pages.Settings
{
    class SecuritySettingsModel
    {
        public bool HelloForLogin
        {
            get => App.RoamingSettings.Read(VERIFY_LOGIN, false);
            set => App.RoamingSettings.Save(VERIFY_LOGIN, value);
        }

        public bool HelloForNSFW
        {
            get => App.RoamingSettings.Read(VERIFY_NSFW, false);
            set => App.RoamingSettings.Save(VERIFY_NSFW, value);
        }

        public bool HelloForSettings
        {
            get => App.RoamingSettings.Read(VERIFY_SETTINGS, false);
            set => App.RoamingSettings.Save(VERIFY_SETTINGS, value);
        }

        public TimeSpan AuthenticationTime
        {
            get => App.RoamingSettings.Read(AUTHENTICATION_TIME, TimeSpan.FromMinutes(5));
            set => App.RoamingSettings.Save(AUTHENTICATION_TIME, value);
        }
    }
}
