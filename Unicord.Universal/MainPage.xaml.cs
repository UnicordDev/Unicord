using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.Services.Store.Engagement;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Unicord.Universal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool IsOverlayShown { get; internal set; }
        public Frame RootFrame => rootFrame;
        public Frame CustomFrame => CustomOverlayFrame;

        private ShareOperation _shareOperation;
        internal MainPageArgs Arguments { get; private set; }

        private RoutedEventHandler _openHandler;
        private RoutedEventHandler _saveHandler;
        private RoutedEventHandler _shareHandler;
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

            WindowManager.HandleTitleBarForWindow(titleBar, this);

            if (_isReady)
            {
                await OnFirstDiscordReady(null);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //var engagementManager = StoreServicesEngagementManager.GetDefault();
            //await engagementManager.RegisterNotificationChannelAsync();

            var pane = InputPane.GetForCurrentView();
            pane.Showing += Pane_Showing;
            pane.Hiding += Pane_Hiding;

            var navigation = SystemNavigationManager.GetForCurrentView();
            navigation.BackRequested += Navigation_BackRequested;

            try
            {
                var vault = new PasswordVault();
                var result = vault.FindAllByResource(Constants.TOKEN_IDENTIFIER).FirstOrDefault(t => t.UserName == "Default");

                if (result != null)
                {
                    connectingOverlay.Visibility = Visibility.Visible;
                    connectingOverlay.Opacity = 1;
                    connectingScale.ScaleX = 1;
                    connectingScale.ScaleY = 1;
                    mainScale.ScaleX = 0.85;
                    mainScale.ScaleY = 0.85;

                    IsOverlayShown = true;
                    connectingProgress.IsIndeterminate = true;

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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var status = StatusBar.GetForCurrentView();
                    status.ProgressIndicator.ProgressValue = NetworkHelper.IsNetworkConnected ? null : (double?)0;
                    status.ProgressIndicator.Text = NetworkHelper.IsNetworkConnected ? "Reconnecting..." : "Offline";
                    await status.ProgressIndicator.ShowAsync();
                }
            });
        }

        private async Task HideDisconnectingMessage()
        {
            Analytics.TrackEvent("Discord_Reconnected");
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var status = StatusBar.GetForCurrentView();
                    await status.ProgressIndicator.HideAsync();
                }
            });
        }

        internal async Task GoToChannelAsync(MainPageArgs args)
        {
            if (args.ChannelId != 0 && App.Discord.TryGetCachedChannel(args.ChannelId, out var channel))
            {
                if (channel.Type == ChannelType.Text && channel.PermissionsFor(channel.Guild.CurrentMember).HasPermission(Permissions.AccessChannels) || channel is DiscordDmChannel)
                {
                    await Dispatcher.AwaitableRunAsync(() =>
                    {
                        rootFrame.Navigate(typeof(ChannelPage), channel);
                        HideConnectingOverlay();
                    });
                }
            }

            try
            {
                var dm = App.Discord.PrivateChannels.Values
                    .FirstOrDefault(c => c.Type == ChannelType.Private && c.Type != ChannelType.Group && c.Recipients.Count == 1 && c.Recipients[0].Id == args.UserId);

                if (dm == null && args.UserId != 0)
                {
                    dm = await App.Discord.CreateDmChannelAsync(args.UserId);
                }

                await Dispatcher.AwaitableRunAsync(() =>
                {
                    rootFrame.Navigate(typeof(ChannelPage), dm);
                    HideConnectingOverlay();
                });
            }
            catch { }
        }

        internal void ShowConnectingOverlay()
        {
            connectingOverlay.Visibility = Visibility.Visible;
            connectingProgress.IsIndeterminate = true;
            IsOverlayShown = true;
            showConnecting.Begin();
        }

        internal void HideConnectingOverlay()
        {
            hideConnecting.Begin();
        }

        private void hideConnecting_Completed(object sender, object e)
        {
            connectingOverlay.Visibility = Visibility.Collapsed;
            connectingProgress.IsIndeterminate = true;
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

                    IProgress<DownloadProgressEventArgs> p = new Progress<DownloadProgressEventArgs>(e => overlayProgressRing.Value = e.Progress);
                    var handler = new DownloadProgressEventHandler((o, e) => p.Report(e));

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
            everything.Margin = new Thickness(0, 0, 0, args.OccludedRect.Height);
            args.EnsuredFocusedElementInView = true;
        }

        private void Pane_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            everything.Margin = new Thickness(0);
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
            CustomOverlayGrid.Visibility = Visibility.Visible;
            ShowOverlayStoryboard.Begin();
        }

        public void HideCustomOverlay()
        {
            HideOverlayStoryboard.Begin();
        }


        private void HideOverlayStoryboard_Completed(object sender, object e)
        {
            CustomOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}
