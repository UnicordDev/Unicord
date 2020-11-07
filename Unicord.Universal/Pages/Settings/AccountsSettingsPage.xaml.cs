using Microsoft.Toolkit.Parsers.Markdown;
using Unicord.Universal.Integration;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Resources;
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

            MarkdownDocument.KnownSchemes.Add("ms-people");

            syncContactsSwitch.IsOn = App.RoamingSettings.Read(SYNC_CONTACTS, true);
            syncContactsSwitch.Toggled += SyncContactsSwitch_Toggled;
        }

        private async void SyncContactsSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            syncContactsSwitch.IsEnabled = false;
            syncingProgressBar.Visibility = Visibility.Visible;
            syncingProgressBar.IsIndeterminate = true;

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
            syncingProgressBar.Visibility = Visibility.Collapsed;
            syncingProgressBar.IsIndeterminate = false;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForCurrentView("AccountsSettingsPage");
            if (await UIUtilities.ShowYesNoDialogAsync(loader.GetString("LogoutPromptTitle"), loader.GetString("LogoutPromptMessage"), "\xF3B1"))
            {
                await App.LogoutAsync();
            }
        }

        private void Md_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {

        }
    }
}
