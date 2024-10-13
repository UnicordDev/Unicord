using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using TenMica;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Models.User;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal
{
    public sealed partial class MainPage : Page
    {
        internal MainPageArgs Arguments { get; private set; }
        public bool IsOverlayShown { get; internal set; }
        public Frame RootFrame => rootFrame;
        public Frame CustomFrame => CustomOverlayFrame;

        private ShareOperation _shareOperation;
        private bool _isReady;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            switch (e.Parameter)
            {
                case MainPageArgs args:
                    Arguments = args;
                    break;
                case ShareOperation operation:
                    _shareOperation = operation;
                    break;
                default:
                    break;
            }


            if (_isReady)
            {
                await OnFirstDiscordReady(null, null);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var handle = WindowingService.Current.GetHandle(this);
            if (WindowingService.Current.IsMainWindow(handle))
                WindowingService.Current.HandleTitleBarForWindow(TitleBar, this);

            if (ThemeService.GetForCurrentView().GetTheme() == AppTheme.SunValley)
            {
                var brush = new TenMicaBrush();
                if (WindowingService.Current.IsCompactOverlay(handle))
                    brush.EnabledInActivatedNotForeground = true;

                Background = brush;
            }

            var pane = InputPane.GetForCurrentView();
            pane.Showing += Pane_Showing;
            pane.Hiding += Pane_Hiding;

            if (Arguments?.SplashScreen != null)
            {
                PositionSplash(Arguments.SplashScreen);
                Window.Current.SizeChanged += OnSplashResize;
            }

            try
            {
                var vault = new PasswordVault();
                var result = vault.FindAllByResource(Constants.TOKEN_IDENTIFIER)
                    .FirstOrDefault(t => t.UserName == "Default");

                if (result != null)
                {
                    rootFrame.Navigate(typeof(DiscordPage));

                    ConnectingOverlay.Visibility = Visibility.Visible;
                    ConnectingOverlay.Opacity = 1;
                    ContentRoot.Opacity = 0;
                    ConnectingScale.ScaleX = 1;
                    ConnectingScale.ScaleY = 1;
                    MainScale.ScaleX = 0.85;
                    MainScale.ScaleY = 0.85;

                    IsOverlayShown = true;
                    ConnectingProgress.IsIndeterminate = true;
                    if (ConnectingProgress1 != null)
                        ConnectingProgress1.IsActive = true;

                    result.RetrievePassword();

                    await DiscordManager.LoginAsync(result.Password, OnFirstDiscordReady, App.LoginError, false);
                }
                else
                {
                    rootFrame.Navigate(typeof(LoginPage));
                    await ClearJumpListAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                rootFrame.Navigate(typeof(LoginPage));
                await ClearJumpListAsync();
            }
        }

        private void OnSplashResize(object sender, WindowSizeChangedEventArgs e)
        {
            PositionSplash(Arguments.SplashScreen);
        }

        private void PositionSplash(SplashScreen splash)
        {
            var imageRect = splash.ImageLocation;
            ExtendedSplashImage.SetValue(Canvas.LeftProperty, imageRect.X);
            ExtendedSplashImage.SetValue(Canvas.TopProperty, imageRect.Y);
            ExtendedSplashImage.Height = imageRect.Height;
            ExtendedSplashImage.Width = imageRect.Width;

            ConnectingProgress.SetValue(Canvas.LeftProperty, imageRect.X + (imageRect.Width * 0.5) - (ConnectingProgress.Width * 0.5));
            ConnectingProgress.SetValue(Canvas.TopProperty, imageRect.Y + imageRect.Height + imageRect.Height * 0.1);
        }

        private void RemoveEventHandlers()
        {
            var pane = InputPane.GetForCurrentView();
            pane.Showing -= Pane_Showing;
            pane.Hiding -= Pane_Hiding;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveEventHandlers();
        }

        private static async Task ClearJumpListAsync()
        {
            if (JumpList.IsSupported())
            {
                var list = await JumpList.LoadCurrentAsync();
                list.Items.Clear();
                await list.SaveAsync();
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                await ContactListManager.ClearContactsAsync();
            }
        }

        private async Task OnFirstDiscordReady(DiscordClient client, ReadyEventArgs e)
        {
            if (!_isReady)
            {
                DiscordManager.Discord.Ready += OnDiscordReady;
                DiscordManager.Discord.Resumed += OnDiscordResumed;
                DiscordManager.Discord.SocketClosed += OnDiscordDisconnected;
                DiscordManager.Discord.LoggedOut += OnLoggedOut;
            }

            DiscordManager.Discord.Ready -= OnFirstDiscordReady;
            Analytics.TrackEvent("Discord_OnFirstReady");

            _isReady = true;

            // TODO: This doesn't work?
            //await e.Client.UpdateStatusAsync(userStatus: UserStatus.Online);

            if (Arguments != null && (Arguments.ChannelId != 0 || Arguments.UserId != 0))
            {
                if (Arguments.FullFrame)
                {
                    await GoToChannelAsync(Arguments);
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        rootFrame.Navigate(typeof(DiscordPage), Arguments));
                }
            }
            else if (_shareOperation != null)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    rootFrame.Navigate(typeof(SharePage), _shareOperation));
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    rootFrame.Navigate(typeof(DiscordPage), Arguments));
            }
        }

        private Task OnLoggedOut(DiscordClient client, LoggedOutEventArgs args)
        {
            _isReady = false;
            RemoveEventHandlers();
            return Task.CompletedTask;
        }

        private async Task OnDiscordReady(DiscordClient client, ReadyEventArgs e)
        {
            await HideDisconnectingMessage();
        }

        private async Task OnDiscordResumed(DiscordClient client, ResumedEventArgs e)
        {
            await HideDisconnectingMessage();
        }

        private async Task OnDiscordDisconnected(DiscordClient client, SocketCloseEventArgs e)
        {
            Analytics.TrackEvent("Discord_Disconnected");
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var status = StatusBar.GetForCurrentView();
                    status.ProgressIndicator.ProgressValue = NetworkHelper.IsNetworkConnected ? null : (double?)0;
                    status.ProgressIndicator.Text = NetworkHelper.IsNetworkConnected ? "Reconnecting..." : "Offline";
                    await status.ProgressIndicator.ShowAsync();
                });
            }
        }

        private async Task HideDisconnectingMessage()
        {
            Analytics.TrackEvent("Discord_Reconnected");
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var status = StatusBar.GetForCurrentView();
                    await status.ProgressIndicator.HideAsync();
                });
            }
        }

        internal async Task GoToChannelAsync(MainPageArgs args)
        {
            try
            {
                if (args.ChannelId != 0)
                {
                    if (DiscordManager.Discord.TryGetCachedChannel(args.ChannelId, out var channel) && channel.IsAccessible())
                    {
                        await DiscordNavigationService.GetForCurrentView()
                            .NavigateAsync(channel);
                    }
                }
                else if (args.UserId != 0)
                {
                    var dm = DiscordManager.Discord.PrivateChannels.Values
                        .FirstOrDefault(c => c.Type == ChannelType.Private && c.Recipients.Count == 1 && c.Recipients[0].Id == args.UserId);

                    await DiscordNavigationService.GetForCurrentView()
                        .NavigateAsync(dm);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            finally
            {
                await Dispatcher.AwaitableRunAsync(() => HideConnectingOverlay());
            }
        }

        internal void ShowConnectingOverlay()
        {
            if (IsOverlayShown)
                return;

            ConnectingOverlay.Visibility = Visibility.Visible;
            ConnectingProgress.IsIndeterminate = true;
            if (ConnectingProgress1 != null)
                ConnectingProgress1.IsActive = true;
            IsOverlayShown = true;
            ShowConnecting.Begin();
        }

        internal void HideConnectingOverlay()
        {
            if (!IsOverlayShown)
                return;

            hideConnecting.Begin();
        }

        private void hideConnecting_Completed(object sender, object e)
        {
            ConnectingOverlay.Visibility = Visibility.Collapsed;
            ConnectingProgress.IsIndeterminate = false;
            if (ConnectingProgress1 != null)
                ConnectingProgress1.IsActive = false;
            IsOverlayShown = false;
        }

        internal void Nagivate(Type type)
        {
            rootFrame.Navigate(type);
        }

        private void Pane_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            Everything.Margin = new Thickness(0, 0, 0, args.OccludedRect.Height);
            args.EnsuredFocusedElementInView = true;
        }

        private void Pane_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            Everything.Margin = new Thickness(0);
            args.EnsuredFocusedElementInView = true;
        }

        public void ShowCustomOverlay()
        {
            WindowingService.Current.HandleTitleBarForControl(CustomOverlayGrid);
            ShowOverlayStoryboard.Begin();
        }

        public void HideCustomOverlay()
        {
            if (CustomOverlayGrid.Visibility != Visibility.Collapsed)
                HideOverlayStoryboard.Begin();
        }

        private void OverlayBackdrop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OverlayService.GetForCurrentView()
                .CloseOverlay();
        }

        private void HideOverlayStoryboard_Completed(object sender, object e)
        {
            CustomOverlayFrame.Navigate(typeof(Page));
        }
    }
}
