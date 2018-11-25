using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private string _token;

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<MainPage>().ShowConnectingOverlay();

            _token = tokenTextBox.Password.Trim('"');
            await App.LoginAsync(_token, Discord_Ready, App.LoginError, false);
        }

        private async Task Discord_Ready(DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential("Unicord_Token", "Default", _token));
            _token = null;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame.Navigate(typeof(DiscordPage));
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var mainPage = this.FindParent<MainPage>();
            if (mainPage.IsOverlayShown)
                mainPage.HideConnectingOverlay();
        }

        private async void loadFileButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<MainPage>().ShowConnectingOverlay();

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".txt");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                var text = await FileIO.ReadTextAsync(file);
                _token = text.Trim('"');
                await App.LoginAsync(_token, Discord_Ready, App.LoginError, false);
            }
            else
            {
                this.FindParent<MainPage>().HideConnectingOverlay();
            }
        }
    }
}
