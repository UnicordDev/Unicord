using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Pages;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    internal enum SettingsPageType
    {
        Home,
        Messaging,
        Themes,
        Media,
        Voice,
        Security,
        About
    }

    internal class SettingsService : BaseService<SettingsService>
    {
        private DiscordPage _discordPage;
        protected override void Initialise()
        {
            _discordPage = Window.Current.Content.FindChild<DiscordPage>();
        }

        public async Task OpenAsync(SettingsPageType page = SettingsPageType.Home)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();
            if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_SETTINGS, loader.GetString("VerifySettingsDisplayReason")))
            {
                _discordPage.OpenSettings(page);
            }
        }
    }
}
