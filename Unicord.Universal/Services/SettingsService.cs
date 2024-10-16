﻿using System.Threading.Tasks;
using Unicord.Universal.Pages.Settings;
using Windows.ApplicationModel.Resources;

namespace Unicord.Universal.Services
{
    internal enum SettingsPageType
    {
        Accounts,
        Messaging, 
        Notifications,
        Themes,
        Media,
        Voice,
        Security,
        About
    }

    internal class SettingsService : BaseService<SettingsService>
    {
        private OverlayService _overlayService;
        //private DiscordPage _discordPage;
        protected override void Initialise()
        {
            _overlayService = OverlayService.GetForCurrentView();
            //_discordPage = Window.Current.Content.FindChild<DiscordPage>();
        }

        public async Task OpenAsync(SettingsPageType page = SettingsPageType.Accounts)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();
            if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_SETTINGS, loader.GetString("VerifySettingsDisplayReason")))
            {
                await _overlayService.ShowOverlayAsync<SettingsPage>(page);
            }
        }
    }
}
