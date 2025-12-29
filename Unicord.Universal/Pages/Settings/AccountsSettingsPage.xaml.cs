using System;
using System.Text;
using Unicord.Universal.Controls;
using Unicord.Universal.Integration;
using Unicord.Universal.Parsers.Markdown;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Unicord.Constants;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class AccountsSettingsPage : Page
    {
        public AccountsSettingsPage()
        {
            InitializeComponent();

            email.Text = GetHiddenEmail(DiscordManager.Discord.CurrentUser.Email);

            MarkdownDocument.KnownSchemes.Add("ms-people");

            syncContactsSwitch.IsOn = App.RoamingSettings.Read(SYNC_CONTACTS, true);
            syncContactsSwitch.Toggled += SyncContactsSwitch_Toggled;
        }

        private async void SyncContactsSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            syncContactsSwitch.IsEnabled = false;

            var isEnabled = App.RoamingSettings.Read(SYNC_CONTACTS, true);
            if (isEnabled)
            {
                await ContactListManager.ClearContactsAsync();
            }
            else
            {
                await ContactListManager.UpdateContactsListAsync();
            }

            App.RoamingSettings.Save(SYNC_CONTACTS, !isEnabled);

            syncContactsSwitch.IsEnabled = true;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForCurrentView("AccountsSettingsPage");
            if (await UIUtilities.ShowYesNoDialogAsync(loader.GetString("LogoutPromptTitle"), loader.GetString("LogoutPromptMessage"), "\xF3B1"))
            {
                await App.LogoutAsync();
            }
        }

        private async void OnMarkdownLinkClicked(object sender, LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }

        private void EmailRevealButton_Click(object sender, RoutedEventArgs e)
        {
            email.Text = DiscordManager.Discord.CurrentUser.Email;

            EmailRevealButton.Visibility = Visibility.Collapsed;
            EmailHideButton.Visibility = Visibility.Visible;
        }

        private void EmailHideButton_Click(object sender, RoutedEventArgs e)
        {
            email.Text = GetHiddenEmail(DiscordManager.Discord.CurrentUser.Email);

            EmailHideButton.Visibility = Visibility.Collapsed;
            EmailRevealButton.Visibility = Visibility.Visible;
        }

        private string GetHiddenEmail(string email)
        {
            var split = email.Split('@');
            var start = split[0].TrimEnd('@');
            var domain = split[1];

            return $"{new string('●', start.Length)}@{domain}";
        }

        private void CopyStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as Models.AccountsSettingsModel;
            if (model == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Friends\t\t\t{model.FriendCountString}");
            sb.AppendLine($"Servers\t\t\t{model.ServerCountString}");
            sb.AppendLine($"Server Channels\t\t{model.ChannelsCountString}");
            sb.AppendLine($"Server Members\t\t{model.MemberCountString}");
            sb.AppendLine($"DM Channels\t\t{model.OpenDMCountString}");
            sb.AppendLine($"Synced Users\t\t{model.SynchedUserCountString}");
            sb.AppendLine($"Synced Presences\t{model.SynchedPresenceCountString}");
            sb.AppendLine($"Emotes\t\t\t{model.EmoteCountString}");

            var dataPackage = new DataPackage();
            dataPackage.SetText(sb.ToString());
            Clipboard.SetContent(dataPackage);
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            var userId = DiscordManager.Discord.CurrentUser?.Id;
            if (userId == null) return;

            var dataPackage = new DataPackage();
            dataPackage.SetText(userId.Value.ToString());
            Clipboard.SetContent(dataPackage);
        }
    }
}
