using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Integration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
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

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                syncContactsSwitch.IsOn = App.RoamingSettings.Read(SYNC_CONTACTS, true);
                syncContactsSwitch.Toggled += SyncContactsSwitch_Toggled;
            }
            else
            {
                syncContactsSwitch.IsEnabled = false;
                contactsUnavailable.Visibility = Visibility.Visible;
            }
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
    }
}
