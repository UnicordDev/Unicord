using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Extensions;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            var dialog = new TokenDialog();
            await dialog.ShowAsync();

            if (!string.IsNullOrWhiteSpace(dialog.Token))
            {
                await TryLoginAsync(dialog.Token);
            }
        }

        private async Task TryLoginAsync(string token)
        {
            var mainPage = this.FindParent<MainPage>();

            try
            {
                token = token.Trim('"').Trim();

                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Your token cannot be empty! If you were logging in via the browser, try using your token.");

                await LoginService.GetForCurrentView()
                    .LoginWithTokenAsync(token)
                    .ConfigureAwait(false);
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
