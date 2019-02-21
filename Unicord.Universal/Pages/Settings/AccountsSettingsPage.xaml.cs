using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Utilities;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AccountsSettingsPage : Page
    {
        public AccountsSettingsPage()
        {
            this.InitializeComponent();
            DataContext = App.Discord.CurrentUser;

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
                await Contacts.ClearContactsAsync();
            }
            else
            {
                await Contacts.UpdateContactsListAsync();
            }

            App.RoamingSettings.Save(SYNC_CONTACTS, !isEnabled);

            syncContactsSwitch.IsEnabled = true;
            syncingProgressBar.Visibility = Visibility.Collapsed;
            syncingProgressBar.IsIndeterminate = false;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (await UIUtilities.ShowYesNoDialogAsync("Are you sure?", "Are you sure you want to logout?", "\xF3B1"))
            {
                await App.Discord.DisconnectAsync();
                App.Discord.Dispose();
                App.Discord = null;

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
    }
}
