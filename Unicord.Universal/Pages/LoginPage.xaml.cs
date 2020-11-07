using System;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Utilities;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class LoginPage : Page
    {
        private string _token;
        private string _tokenId;

        public LoginPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string s)
            {
                _tokenId = s;
            }
            else
            {
                _tokenId = "Default";
            }
        }

        private string ValidateToken(string token)
        {
            token = token.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Your token can't be empty!");

            return token;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();

            try
            {
                mainPage.ShowConnectingOverlay();
                _token = ValidateToken(tokenTextBox.Password);
                await App.LoginAsync(_token, OnReady, App.LoginError, false);
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("Failed to login!", ex.Message);
                mainPage.HideConnectingOverlay();
            }
        }

        private async void loadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();

            try
            {
                mainPage.ShowConnectingOverlay();

                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".txt");
                var file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    var text = await FileIO.ReadTextAsync(file);
                    _token = ValidateToken(text);
                    await App.LoginAsync(_token, OnReady, App.LoginError, false);
                }
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("Failed to login!", ex.Message);
                mainPage.HideConnectingOverlay();
            }
        }

        private async Task OnReady(ReadyEventArgs e)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, _tokenId, _token));
            _token = null;

            if (_tokenId == "Default")
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(DiscordPage));
                });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            if (mainPage.IsOverlayShown)
            {
                mainPage.HideConnectingOverlay();
            }
        }
    }
}
