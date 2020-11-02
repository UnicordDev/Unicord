using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Models;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.GameBar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameBarMainPage : Page
    {
        public GameBarMainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectingProgress.IsIndeterminate = true;


            var vault = new PasswordVault();
            var result = vault.FindAllByResource(Constants.TOKEN_IDENTIFIER).FirstOrDefault(t => t.UserName == "Default");

            if (result != null)
            {
                result.RetrievePassword();
                await App.LoginAsync(result.Password, OnFirstDiscordReady, OnLoginError, false);
            }
            else
            {
                Frame.Navigate(typeof(NotLoggedInPage));
            }
        }

        private async Task OnFirstDiscordReady(ReadyEventArgs e)
        {
            await Dispatcher.AwaitableRunAsync(() =>
            {
                ConnectingProgress.IsIndeterminate = false;
                DataContext = new FriendsViewModel();
            });
        }

        private async Task OnLoginError(Exception arg)
        {
            await Dispatcher.AwaitableRunAsync(() => Frame.Navigate(typeof(NotLoggedInPage)));
        }
    }
}
