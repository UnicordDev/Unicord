using DSharpPlus.EventArgs;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.GameBar
{
    public record GameBarPageParameters(XboxGameBarWidget Widget, Type PageType, Uri Url);

    public sealed partial class GameBarMainPage : Page
    {
        private GameBarPageParameters _params;

        public GameBarMainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _params = (GameBarPageParameters)e.Parameter;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var vault = new PasswordVault();
                var result = vault.FindAllByResource(Constants.TOKEN_IDENTIFIER).FirstOrDefault(t => t.UserName == "Default");

                if (result != null)
                {
                    result.RetrievePassword();
                    await App.LoginAsync(result.Password, OnFirstDiscordReady, OnLoginError, false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            Frame.Navigate(typeof(NotLoggedInPage));
        }

        private async Task OnFirstDiscordReady(ReadyEventArgs e)
        {
            await Dispatcher.AwaitableRunAsync(() => Frame.Navigate(_params.PageType, _params));
        }

        private async Task OnLoginError(Exception arg)
        {
            await Dispatcher.AwaitableRunAsync(() => Frame.Navigate(typeof(NotLoggedInPage)));
        }
    }
}
