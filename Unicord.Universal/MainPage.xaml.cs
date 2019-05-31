using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using WamWooWam.Core;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.UI.Core;
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
        private MainPageArgs _args;

        private RoutedEventHandler _openHandler;
        private RoutedEventHandler _saveHandler;
        private RoutedEventHandler _shareHandler;
        private bool _isReady;
        private bool _visibility;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _visibility = e.Visible;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            switch (e.Parameter)
            {
                case MainPageArgs args:
                    _args = args;
                    break;
                case ShareOperation operation:
                    _shareOperation = operation;
                    break;
                default:
                    break;
            }

            _visibility = Window.Current.Visible;
            HandleTitleBar();

            if (_isReady)
            {
                await Discord_Ready(null);
            }
        }

        private void HandleTitleBar()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                void UpdateTitleBarLayout(CoreApplicationViewTitleBar bar)
                {
                    titleBar.Height = bar.Height;
                    App.StatusBarFill = new Thickness(0, bar.Height, 0, 0);
                }

                var baseTitlebar = ApplicationView.GetForCurrentView().TitleBar;
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
                coreTitleBar.LayoutMetricsChanged += (o, ev) => UpdateTitleBarLayout(o);
                baseTitlebar.ButtonBackgroundColor = Colors.Transparent;
                baseTitlebar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.Visibility = Visibility.Visible;

                UpdateTitleBarLayout(coreTitleBar);

                Window.Current.SetTitleBar(titleBar);
            }

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = StatusBar.GetForCurrentView();
                if (statusBar != null)
                {
                    statusBar.BackgroundOpacity = 0;

                    App.StatusBarFill = new Thickness(0, 25, 0, 0);

                    var applicationView = ApplicationView.GetForCurrentView();
                    applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var pane = InputPane.GetForCurrentView();
            pane.Showing += Pane_Showing;
            pane.Hiding += Pane_Hiding;

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

                    await App.LoginAsync(result.Password, Discord_Ready, App.LoginError, false);
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

        private async Task Discord_Ready(ReadyEventArgs e)
        {
            _isReady = true;

            // TODO: This doesn't work?
            //await e.Client.UpdateStatusAsync(userStatus: UserStatus.Online);

            if (_args != null)
            {
                if (_args.FullFrame)
                {
                    await GoToChannelAsync(App.Discord);
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        rootFrame.Navigate(typeof(DiscordPage), _args));
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

        internal async Task GoToChannelAsync(DiscordClient e)
        {
            var guildChannels = e.Guilds.Values.SelectMany(g => g.Channels.Values)
                .Where(c => c.Type == ChannelType.Text && c.PermissionsFor(c.Guild.CurrentMember).HasPermission(Permissions.AccessChannels));

            var dm = e.PrivateChannels.Values
                .Concat(guildChannels)
                .FirstOrDefault(c => (c is DiscordDmChannel d && d.Type == ChannelType.Private) ? d.Recipient.Id == _args.UserId || c.Id == _args.ChannelId : c.Id == _args.ChannelId);

            if (dm == null && _args.UserId != 0)
            {
                dm = await App.Discord.CreateDmChannelAsync(_args.UserId);
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rootFrame.Navigate(typeof(ChannelPage), dm);
                HideConnectingOverlay();
            });
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

            parent.Children.Remove(element);
            fullscreenCanvas.Children.Add(element);
            element.Width = double.NaN;
            element.Height = double.NaN;
        }

        internal void LeaveFullscreen()
        {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

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
    }
}
