using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
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
        private RoutedEventHandler _openHandler;
        private RoutedEventHandler _saveHandler;
        private RoutedEventHandler _shareHandler;
        private bool _isReady;

        public MainPage()
        {
            InitializeComponent();
#if DEBUG
            this.AddAccelerator(Windows.System.VirtualKey.C, Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift, (_, _) =>
            {
                if (IsOverlayShown) 
                {
                    HideConnectingOverlay();
                }
                else
                {
                    ShowConnectingOverlay();
                }
            });
#endif
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
                await OnFirstDiscordReady(null);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowingService.Current.HandleTitleBarForWindow(TitleBar, this);

            //var engagementManager = StoreServicesEngagementManager.GetDefault();
            //await engagementManager.RegisterNotificationChannelAsync();

            var pane = InputPane.GetForCurrentView();
            pane.Showing += Pane_Showing;
            pane.Hiding += Pane_Hiding;

            var navigation = SystemNavigationManager.GetForCurrentView();
            navigation.BackRequested += Navigation_BackRequested;

            if (Arguments?.SplashScreen != null)
            {
                PositionSplash(Arguments.SplashScreen);
                Window.Current.SizeChanged += OnSplashResize;
            }

            try
            {
                var vault = new PasswordVault();
                var result = vault.FindAllByResource(Constants.TOKEN_IDENTIFIER).FirstOrDefault(t => t.UserName == "Default");

                if (result != null)
                {
                    ConnectingOverlay.Visibility = Visibility.Visible;
                    ConnectingOverlay.Opacity = 1;
                    ContentRoot.Opacity = 0;
                    ConnectingScale.ScaleX = 1;
                    ConnectingScale.ScaleY = 1;
                    MainScale.ScaleX = 0.85;
                    MainScale.ScaleY = 0.85;

                    IsOverlayShown = true;
                    ConnectingProgress.IsIndeterminate = true;

                    result.RetrievePassword();

                    await App.LoginAsync(result.Password, OnFirstDiscordReady, App.LoginError, false);
                }
                else
                {
                    rootFrame.Navigate(typeof(LoginPage));
                    await ClearJumpListAsync();
                }
            }
            catch (Exception ex)
            {
                rootFrame.Navigate(typeof(LoginPage));
                await ClearJumpListAsync();
            }
        }

        private void OnSplashResize(object sender, WindowSizeChangedEventArgs e)
        {
            PositionSplash(Arguments.SplashScreen);
        }

        private void PositionSplash(Windows.ApplicationModel.Activation.SplashScreen splash)
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

            var navigation = SystemNavigationManager.GetForCurrentView();
            navigation.BackRequested -= Navigation_BackRequested;

            if (App.Discord != null)
            {
                App.Discord.Ready -= OnDiscordReady;
                App.Discord.Resumed -= OnDiscordResumed;
                App.Discord.SocketClosed -= OnDiscordDisconnected;
                App.Discord.LoggedOut -= OnLoggedOut;
            }
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

        internal void ShowUserOverlay(DiscordUser user, bool animate)
        {
            userInfoOverlay.User = user;
            userInfoOverlay.Visibility = Visibility.Visible;
            userInfoPopup.Visibility = Visibility.Visible;

            showUserOverlay.Begin();
        }

        private async Task OnFirstDiscordReady(ReadyEventArgs e)
        {
            if (!_isReady)
            {
                App.Discord.Ready += OnDiscordReady;
                App.Discord.Resumed += OnDiscordResumed;
                App.Discord.SocketClosed += OnDiscordDisconnected;
                App.Discord.LoggedOut += OnLoggedOut;
            }

            App.Discord.Ready -= OnFirstDiscordReady;
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

        private Task OnLoggedOut()
        {
            _isReady = false;
            RemoveEventHandlers();
            return Task.CompletedTask;
        }

        private async Task OnDiscordReady(ReadyEventArgs e)
        {
            await HideDisconnectingMessage();
        }

        private async Task OnDiscordResumed(ReadyEventArgs e)
        {
            await HideDisconnectingMessage();
        }

        private async Task OnDiscordDisconnected(SocketCloseEventArgs e)
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
                    if (App.Discord.TryGetCachedChannel(args.ChannelId, out var channel) && channel.IsAccessible())
                    {
                        await Dispatcher.AwaitableRunAsync(() => rootFrame.Navigate(typeof(ChannelPage), channel));
                    }
                }
                else if (args.UserId != 0)
                {
                    var dm = App.Discord.PrivateChannels.Values
                        .FirstOrDefault(c => c.Type == ChannelType.Private && c.Recipients.Count == 1 && c.Recipients[0].Id == args.UserId);

                    if (dm == null && args.UserId != 0)
                    {
                        // dm = await App.Discord.CreateDmChannelAsync(args.UserId);
                    }

                    await Dispatcher.AwaitableRunAsync(() => rootFrame.Navigate(typeof(ChannelPage), dm));
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
            IsOverlayShown = false;
        }

        internal void Nagivate(Type type)
        {
            rootFrame.Navigate(type);
        }

        internal void ShowAttachmentOverlay(Uri url, int width, int height, RoutedEventHandler openHandler, RoutedEventHandler saveHandler, RoutedEventHandler shareHandler)
        {
            if (attachmentImage.Source == null)
            {
                contentContainerOverlay.Visibility = Visibility.Visible;
                subText.Visibility = Visibility.Visible;
                contentOverlay.Visibility = Visibility.Visible;
                showContent.Begin();

                _openHandler = openHandler;
                _saveHandler = saveHandler;
                _shareHandler = shareHandler;
                openButton.Click += _openHandler;
                saveButton.Click += _saveHandler;
                shareButton.Click += _shareHandler;

                scaledControl.TargetWidth = width;
                scaledControl.TargetHeight = height;
                attachmentImage.MaxWidth = width;
                attachmentImage.MaxHeight = height;

                if ((attachmentImage.Source as BitmapImage)?.UriSource != url)
                {
                    var src = new BitmapImage();
                    attachmentImage.Source = src;

                    overlayProgressRing.Value = 0;
                    contentContainerOverlay.Visibility = Visibility.Visible;

                    var p = new Progress<int>(e => overlayProgressRing.Value = e);
                    var handler = new DownloadProgressEventHandler((o, e) => ((IProgress<int>)p).Report(e.Progress));

                    src.DownloadProgress += handler;
                    src.ImageOpened += (o, e) =>
                    {
                        contentContainerOverlay.Visibility = Visibility.Collapsed;
                        src.DownloadProgress -= handler;
                    };

                    src.UriSource = url;
                }
            }
            else
            {
                ResetOverlay();
                ShowAttachmentOverlay(url, width, height, openHandler, saveHandler, shareHandler);
            }
        }

        public void HideOverlay()
        {
            if (contentOverlay.Visibility == Visibility.Visible)
            {
                hideContent.Begin();
            }

            if (userInfoOverlay.Visibility == Visibility.Visible)
            {
                hideUserOverlay.Begin();
            }
        }

        private void ResetOverlay()
        {
            contentOverlay.Visibility = Visibility.Collapsed;

            if (_openHandler != null)
            {
                openButton.Click -= _openHandler;
            }

            if (_saveHandler != null)
            {
                saveButton.Click -= _saveHandler;
            }

            if (_shareHandler != null)
            {
                shareButton.Click -= _shareHandler;
            }

            _openHandler = null;
            _saveHandler = null;
            _shareHandler = null;

            attachmentImage.Source = null;
        }

        private void hideContent_Completed(object sender, object e)
        {
            ResetOverlay();
        }

        private void contentOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            HideOverlay();
        }

        private void hideUserOverlay_Completed(object sender, object e)
        {
            userInfoOverlay.Visibility = Visibility.Collapsed;
            userInfoPopup.Visibility = Visibility.Collapsed;
        }

        private void userInfoPopup_Tapped(object sender, TappedRoutedEventArgs e)
        {
            hideUserOverlay.Begin();
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

        private void Navigation_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (contentOverlay.Visibility == Visibility.Visible)
            {
                e.Handled = true;
                hideContent.Begin();
            }

            if (userInfoOverlay.Visibility == Visibility.Visible)
            {
                e.Handled = true;
                hideUserOverlay.Begin();
            }
        }

        public void ShowCustomOverlay()
        {
            WindowingService.Current.HandleTitleBarForControl(CustomOverlayGrid);
            ShowOverlayStoryboard.Begin();
        }

        public void HideCustomOverlay()
        {
            HideOverlayStoryboard.Begin();
        }
    }
}
