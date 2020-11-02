using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Toolkit.Parsers.Markdown;
using Microsoft.Toolkit.Uwp.UI;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
                await ImageCache.Instance.ClearAsync();
                await App.Discord.DisconnectAsync();
                App.Discord.Dispose();

                try
                {
                    var passwordVault = new PasswordVault();
                    foreach (var c in passwordVault.FindAllByResource(TOKEN_IDENTIFIER))
                    {
                        passwordVault.Remove(c);
                    }
                }
                catch { }

                var frame = (Window.Current.Content as Frame);
                frame.Navigate(typeof(Page));
                frame.BackStack.Clear();
                frame.ForwardStack.Clear();
                frame.Navigate(typeof(MainPage));
            }
        }

        private void Md_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {

        }
    }
}
