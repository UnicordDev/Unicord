using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void TokenLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            mainPage?.ShowConnectingOverlay();

            var dialog = new TokenDialog();
            await dialog.ShowAsync();

            if (!string.IsNullOrWhiteSpace(dialog.Token))
            {
                await TryLoginAsync(dialog.Token);
            }
            else
            {
                mainPage?.HideConnectingOverlay();
            }
        }

        private async Task TryLoginAsync(string token)
        {
            Task OnReady(DiscordClient client, ReadyEventArgs e)
            {
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, "Default", token));

                return Task.CompletedTask;
            }

            var mainPage = this.FindParent<MainPage>();

            try
            {
                token = token.Trim('"').Trim();

                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Your token cannot be empty! If you were logging in via the browser, try using your token.");

                mainPage.ShowConnectingOverlay();
                Frame.Navigate(typeof(DiscordPage));
                await DiscordManager.LoginAsync(token, OnReady, App.LoginError, false);
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("Failed to login!", ex.Message);
                mainPage.HideConnectingOverlay();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            mainPage.HideConnectingOverlay();
        }
    }
}
