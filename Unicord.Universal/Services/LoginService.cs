using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI;
using Windows.Security.Credentials;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Microsoft.Toolkit.Uwp.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Extensions;
using Windows.UI.Core;

namespace Unicord.Universal.Services
{


    internal class LoginService : BaseService<LoginService>
    {
        public bool HasToken
            => TryGetToken(out _);

        public async Task LoginAsync()
        {
            if (TryGetToken(out var token))
            {
                await LoginWithTokenAsync(token);
                return;
            }

        }

        public async Task LoginWithTokenAsync(string token)
        {
            await DiscordManager.ConnectAsync(token);
            await DiscordManager.WaitForReadyAsync();
        }

        public async Task LogoutAsync()
        {
            await Task.WhenAll(WeakReferenceMessenger.Default.Send<LoggedOutMessage>());

            await WebView.ClearTemporaryWebDataAsync();
            await WindowingService.Current.CloseAllWindowsAsync();
            await ImageCache.Instance.ClearAsync();
            await DiscordManager.LogoutAsync();

            try
            {
                var passwordVault = new PasswordVault();
                foreach (var c in passwordVault.FindAllByResource(Constants.TOKEN_IDENTIFIER))
                {
                    passwordVault.Remove(c);
                }
            }
            catch { }

            // ditto above about the background process
            App.LocalSettings.TryDelete("Token");

            DiscordNavigationService.Reset();
            FullscreenService.Reset();
            OverlayService.Reset();
            SettingsService.Reset();
            SwipeOpenService.Reset();
            BackgroundNotificationService.Reset();

            foreach (var view in CoreApplication.Views)
            {
                if (view.IsMain)
                {
                    await view.Dispatcher.AwaitableRunAsync(() =>
                    {
                        var frame = (Frame)Window.Current.Content;
                        frame.Navigate(typeof(Page));
                        frame.BackStack.Clear();
                        frame.ForwardStack.Clear();

                        frame = new Frame();
                        frame.Navigate(typeof(MainPage));
                        Window.Current.Content = frame;
                    });
                }
            }
        }

        internal static bool TryGetToken(out string token)
        {
            try
            {
                var passwordVault = new PasswordVault();
                var credential = passwordVault.Retrieve(Constants.TOKEN_IDENTIFIER, "Default");
                credential.RetrievePassword();

                token = credential.Password;
                return true;
            }
            catch { }

            token = null;
            return false;
        }
    }
}
