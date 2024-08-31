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
        private const string LOGIN_URL = "https://discordapp.com/login";
        private const string TOKEN_NABBER = "(()=>{let e=document.createElement('iframe');document.body.appendChild(e);return e.contentWindow.localStorage.getItem('token')})();";
        private const string HISTORY_INTERCEPTOR = "(()=>{let t=history.pushState;history.pushState=((...a)=>{t.apply(history,a),window.external.notify('')})})();";

        private bool _browserVisible;
        private WebView _browser;

        public LoginPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_browser != null)
            {
                HideBrowser();
                _browser.Navigate(new Uri("about:blank"));
                _browser = null;
                BrowserContainer.Content = null;
            }
        }

        private void ShowBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            ShowBrowser();
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
            async Task OnReady(DiscordClient client, ReadyEventArgs e)
            {
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(Constants.TOKEN_IDENTIFIER, "Default", token));

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(DiscordPage));
                });
            }

            var mainPage = this.FindParent<MainPage>();

            try
            {
                token = token.Trim('"').Trim();

                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Your token cannot be empty! If you were logging in via the browser, try using your token.");

                mainPage.ShowConnectingOverlay();
                await App.LoginAsync(token, OnReady, App.LoginError, false);
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("Failed to login!", ex.Message);
                mainPage.HideConnectingOverlay();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                BrowserTitlebarContent.Visibility = Visibility.Collapsed;

            WindowingService.Current.HandleTitleBarForWindow(BrowserTitlebarGrid, null);

            var mainPage = this.FindParent<MainPage>();
            mainPage.HideConnectingOverlay();
        }

        void ShowBrowser()
        {
            if (_browserVisible)
                return;

            BrowserGrid.Visibility = Visibility.Visible;

            if (_browser == null)
            {
                var executionMode = WebViewExecutionMode.SameThread;
                if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.WebViewExecutionMode", "SeparateProcess"))
                    executionMode = WebViewExecutionMode.SeparateProcess;

                _browser = new WebView(executionMode);
                _browser.NavigationStarting += OnNavigationStarting;
                _browser.NavigationCompleted += OnNavigationCompleted;
                _browser.ScriptNotify += OnScriptNotify;

                BrowserContainer.Content = _browser;
                _browser.Navigate(new Uri(LOGIN_URL));
            }

            _browserVisible = true;

            ShowBrowserStoryboard.Begin();
        }

        void HideBrowser()
        {
            if (!_browserVisible)
                return;

            HideBrowserStoryboard.Begin();
        }

        private async void OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BrowserProgress.IsIndeterminate = true;
                UpdateUri(args.Uri);
            });
        }

        private async void OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (_browser == null)
                return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BrowserProgress.IsIndeterminate = false;
                UpdateUri(args.Uri);
            });

            await _browser.InvokeScriptAsync("eval", new[] { HISTORY_INTERCEPTOR });

            // only inject into discord just to be sure
            if (args.Uri.Host.EndsWith("discord.com") && !args.Uri.AbsolutePath.StartsWith("/login"))
            {
                await NabTokenAsync();
            }
        }

        private async void OnScriptNotify(object sender, NotifyEventArgs e)
        {
            UpdateUri(e.CallingUri);
            if (e.CallingUri.Host.EndsWith("discord.com") && !e.CallingUri.AbsolutePath.StartsWith("/login"))
            {
                await NabTokenAsync();
            }
        }

        private void UpdateUri(Uri uri)
        {
            if (uri != null)
            {
                ProtocolRun.Text = uri.GetLeftPart(UriPartial.Scheme);
                HostnameRun.Text = uri.Host;
                PathRun.Text = uri.PathAndQuery;
            }
        }

        private async Task NabTokenAsync()
        {
            var token = await _browser.InvokeScriptAsync("eval", new[] { TOKEN_NABBER });
            if (!string.IsNullOrWhiteSpace(token))
            {
                HideBrowser();
                await TryLoginAsync(token);
            }
        }

        private void HideBrowserStoryboard_Completed(object sender, object e)
        {
            _browserVisible = false;
            BrowserGrid.Visibility = Visibility.Collapsed;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_browserVisible)
                BrowserTranslateTransform.Y = this.ActualHeight;
            HideBrowserAnimation.To = this.ActualHeight;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_browser?.CanGoBack == true)
                _browser.GoBack();
            else
                HideBrowser();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _browser?.Refresh();
        }
    }
}
