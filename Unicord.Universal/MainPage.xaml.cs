using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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

        private ShareOperation _shareOperation;
        internal MainPageArgs Arguments { get; private set; }

        private RoutedEventHandler _openHandler;
        private RoutedEventHandler _saveHandler;
        private RoutedEventHandler _shareHandler;
        private bool _isReady;
        private FrameworkElement _fullscreenElement;
        private Panel _fullscreenParent;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
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

            WindowManager.HandleTitleBarForWindow(titleBar);

            if (_isReady)
            {
                await OnFirstDiscordReady(null);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
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
            catch
            {
                rootFrame.Navigate(typeof(LoginPage));
                await ClearJumpListAsync();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            var pane = InputPane.GetForCurrentView();
            pane.Showing -= Pane_Showing;
            pane.Hiding -= Pane_Hiding;
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

        internal void ShowGuildOverlay(DiscordGuild guild, bool animate)
        {
            // TODO: Guild Overlay
        }

        private async Task OnFirstDiscordReady(ReadyEventArgs e)
        {
            _isReady = true;

            App.Discord.Ready += OnDiscordReady;
            App.Discord.Resumed += OnDiscordResumed;
            App.Discord.SocketClosed += OnDiscordDisconnected;

            // TODO: This doesn't work?
            //await e.Client.UpdateStatusAsync(userStatus: UserStatus.Online);

            if (Arguments != null && (Arguments.ChannelId != 0 || Arguments.UserId != 0))
            {
                if (Arguments.FullFrame)
                {
                    await GoToChannelAsync(App.Discord);
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
                    rootFrame.Navigate(typeof(DiscordPage)));
            }
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var status = StatusBar.GetForCurrentView();
                    status.ProgressIndicator.ProgressValue = null;
                    status.ProgressIndicator.Text = "Reconnecting...";
                    await status.ProgressIndicator.ShowAsync();
                }
            });
        }

        private async Task HideDisconnectingMessage()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var status = StatusBar.GetForCurrentView();
                    await status.ProgressIndicator.HideAsync();
                }
            });
        }

        internal async Task GoToChannelAsync(DiscordClient e)
        {
            if (Arguments.ChannelId != 0 && e._channelCache.TryGetValue(Arguments.ChannelId, out var channel))
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
                var dm = e.PrivateChannels.Values
                    .FirstOrDefault(c => c.Recipient?.Id == Arguments.UserId);

                if (dm == null && Arguments.UserId != 0)
                {
                    dm = await App.Discord.CreateDmChannelAsync(Arguments.UserId);
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

                Drawing.ScaleProportions(ref width, ref height, (int)ActualWidth - 80, (int)ActualHeight - (int)(80 + subText.ActualHeight));

                attachmentImage.Width = width;
                attachmentImage.Height = height;
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

        internal void EnterFullscreen(FrameworkElement element, Panel parent)
        {
            var view = ApplicationView.GetForCurrentView();
            view.TryEnterFullScreenMode();

            fullscreenCanvas.Visibility = Visibility.Visible;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped | DisplayOrientations.Portrait;

            _fullscreenElement = element;
            _fullscreenParent = parent;

            parent.Children.Remove(element);
            fullscreenCanvas.Children.Add(element);
            element.Width = double.NaN;
            element.Height = double.NaN;
        }

        internal void LeaveFullscreen()
        {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            _fullscreenElement = null;
            _fullscreenParent = null;

            fullscreenCanvas.Children.Clear();
            fullscreenCanvas.Visibility = Visibility.Collapsed;
        }

        internal void LeaveFullscreen(FrameworkElement element, Panel parent)
        {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            fullscreenCanvas.Children.Remove(element);
            parent.Children.Insert(0, element);
            element.Width = double.NaN;
            element.Height = double.NaN;

            _fullscreenElement = null;
            _fullscreenParent = null;

            fullscreenCanvas.Visibility = Visibility.Collapsed;
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
            if (fullscreenCanvas.Visibility == Visibility.Visible)
            {
                if (_fullscreenElement != null && _fullscreenParent != null)
                {
                    LeaveFullscreen(_fullscreenElement, _fullscreenParent);
                }
                else
                {
                    LeaveFullscreen();
                }

                e.Handled = true;
            }

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
    }
}
