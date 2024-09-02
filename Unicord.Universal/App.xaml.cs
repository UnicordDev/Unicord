using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
#if XBOX_GAME_BAR
using Microsoft.Gaming.XboxGameBar;
using Unicord.Universal.Pages.GameBar;
#endif
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using Unicord.Universal.Integration;
using Unicord.Universal.Misc;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static Unicord.Constants;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace Unicord.Universal
{
    sealed partial class App : Application
    {
#if XBOX_GAME_BAR
        private static XboxGameBarWidget _chatListWidget;
        private static XboxGameBarWidget _friendsListWidget;
#endif

        private static SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource = new TaskCompletionSource<ReadyEventArgs>();

        internal static DiscordClient Discord { get; set; }
        internal static ApplicationDataStorageHelper LocalSettings { get; } = ApplicationDataStorageHelper.GetCurrent();
        internal static ApplicationDataStorageHelper RoamingSettings { get; } = ApplicationDataStorageHelper.GetCurrent();

        public App()
        {
            InitializeComponent();

            var provider = VersionHelper.RegisterVersionProvider<UnicordVersionProvider>();
            var theme = (ElementTheme)LocalSettings.Read(REQUESTED_COLOUR_SCHEME, (int)ElementTheme.Default);
            switch (theme)
            {
                case ElementTheme.Light:
                    RequestedTheme = ApplicationTheme.Light;
                    break;
                case ElementTheme.Dark:
                    RequestedTheme = ApplicationTheme.Dark;
                    break;
            }

            Suspending += OnSuspending;
            UnhandledException += OnUnhandledException;

            Debug.WriteLine("Welcome to Unicord!");
            Debug.WriteLine(provider.GetVersionString());
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.LogError(e.Exception);
            Logger.Log(e.Message);
            e.Handled = true;
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            switch (e)
            {
                case ContactPanelActivatedEventArgs cont:
                    await OnContactPanelActivated(cont);
                    return;
                case ToastNotificationActivatedEventArgs toast:
                    OnLaunched(false, toast.Argument);
                    return;
                case ProtocolActivatedEventArgs protocol:
                    OnProtocolActivatedAsync(protocol);
                    return;
#if XBOX_GAME_BAR
                case XboxGameBarWidgetActivatedEventArgs xbox:
                    OnXboxGameBarActivated(xbox);
                    return;
#endif
                default:
                    Debug.WriteLine(e.Kind);
                    break;
            }
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            switch (args.TaskInstance.Task.Name)
            {
                case TOAST_BACKGROUND_TASK_NAME:
                    if (args.TaskInstance.TriggerDetails is not ToastNotificationActionTriggerDetail details || !TryGetToken(out var token))
                        break;

                    var arguments = ParseArgs(details.Argument);
                    var userInput = details.UserInput;

                    if (!arguments.TryGetValue("channelId", out var cId) || !ulong.TryParse(cId, out var channelId))
                        break;

                    if (!userInput.TryGetValue("tbReply", out var t) || t is not string text)
                        break;
                    // channelId, text, false, null, Enumerable.Empty<IMention>(), null
                    var client = new DiscordRestClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.User });
                    await client.CreateMessageAsync(channelId, new DiscordMessageBuilder().WithContent(text));
                    break;
            }

            deferral.Complete();
        }

#if XBOX_GAME_BAR
        private void OnXboxGameBarActivated(XboxGameBarWidgetActivatedEventArgs xbox)
        {
            Analytics.TrackEvent("Unicord_LaunchForGameBar");

            if (xbox.IsLaunchActivation)
            {
                var name = xbox.Uri.LocalPath;
                var frame = new Frame();
                frame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = frame;

                Logger.Log(xbox.Uri.LocalPath);

                if (name == "unicord-friendslist")
                {
                    _friendsListWidget = new XboxGameBarWidget(xbox, Window.Current.CoreWindow, frame);
                    Window.Current.Closed += OnFrendsListWidgetClosed;

                    frame.Navigate(typeof(GameBarMainPage), new GameBarPageParameters(_friendsListWidget, typeof(GameBarFriendsPage), xbox.Uri));
                }

                if (name == "unicord-channel")
                {
                    _chatListWidget = new XboxGameBarWidget(xbox, Window.Current.CoreWindow, frame);
                    Window.Current.Closed += OnChatListWidgetClosed;

                    frame.Navigate(typeof(GameBarMainPage),  new GameBarPageParameters(_chatListWidget, typeof(GameBarChannelListPage), xbox.Uri));
                }
            }
        }

        private void OnChatListWidgetClosed(object sender, CoreWindowEventArgs e)
        {
            Window.Current.Closed -= OnChatListWidgetClosed;
            _chatListWidget = null;
        }

        private void OnFrendsListWidgetClosed(object sender, CoreWindowEventArgs e)
        {
            Window.Current.Closed -= OnFrendsListWidgetClosed;
            _friendsListWidget = null;
        }
#endif

        private void OnProtocolActivatedAsync(ProtocolActivatedEventArgs protocol)
        {
            if (protocol.Uri.IsAbsoluteUri)
                Analytics.TrackEvent("Unicord_LaunchForProtocol", new Dictionary<string, string>() { ["protocol"] = protocol.Uri.GetLeftPart(UriPartial.Authority) });

            if (protocol.Uri.AbsolutePath.Trim('/').StartsWith("channels"))
            {
                var path = protocol.Uri.AbsolutePath.Split('/').Skip(1).ToArray();
                if (path.Length > 1 && ulong.TryParse(path[2], out var channel))
                {
                    if (!(Window.Current.Content is Frame rootFrame))
                    {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }

                    rootFrame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel, FullFrame = false, IsUriActivation = true });
                    Window.Current.Activate();
                    return;
                }
            }
            else
            {
                OnLaunched(false, "");
            }
        }

        private static async Task OnContactPanelActivated(ContactPanelActivatedEventArgs task)
        {
            Analytics.TrackEvent("Unicord_LaunchForMyPeople");

            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                try
                {
                    var id = await ContactListManager.TryGetChannelIdAsync(task.Contact);
                    if (id != 0)
                    {
                        rootFrame.Navigate(typeof(MainPage), new MainPageArgs() { UserId = id, FullFrame = true, IsUriActivation = false });
                    }
                }
                catch
                {
                    Analytics.TrackEvent("Unicord_MyPeopleFailedToFindPerson");
                    var dialog = new MessageDialog("Something went wrong trying to find this person, sorry!", "Whoops!");
                    await dialog.ShowAsync();
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunched(e.PrelaunchActivated, e.Arguments, e.PreviousExecutionState);
        }

        private void OnLaunched(bool preLaunch, string arguments, ApplicationExecutionState previousState = ApplicationExecutionState.NotRunning)
        {
            var channelId = 0ul;
            Exception themeLoadException = null;
            var args = ParseArgs(arguments);

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                Analytics.TrackEvent("Unicord_Launch");

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousState == ApplicationExecutionState.Terminated)
                    channelId = LocalSettings.Read("LastViewedChannel", 0ul);                

                WindowingService.Current.SetMainWindow(rootFrame);
                Window.Current.Content = rootFrame;
            }

            if (args.TryGetValue("channelId", out var id) && ulong.TryParse(id, out var pId))
            {
                channelId = pId;
            }

            if (rootFrame.Content == null || channelId != 0)
                rootFrame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channelId, ThemeLoadException = themeLoadException });

            // Ensure the current window is active
            Window.Current.Activate();
        }

        private static Dictionary<string, string> ParseArgs(string arguments)
        {
            var rawArgs = Strings.SplitCommandLine(arguments);
            var args = new Dictionary<string, string>();
            foreach (var str in rawArgs)
            {
                if (!string.IsNullOrWhiteSpace(str) && str.Contains('-', '='))
                {
                    var arg = str.TrimStart('-');
                    args.Add(arg.Substring(0, arg.IndexOf('=')), arg.Substring(arg.IndexOf('=') + 1));
                }
            }

            return args;
        }

        private bool TryGetToken(out string token)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("Token", out var s))
            {
                token = (string)s;
                return true;
            }

            token = null;
            return false;
        }

        private static ApplicationView SetupCurrentView()
        {
            var view = ApplicationView.GetForCurrentView();
            view.SetPreferredMinSize(new Size(480, 480));

            var frame = new Frame();
            Window.Current.Content = frame;
            Window.Current.Activate();
            return view;
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                Analytics.TrackEvent("Unicord_LaunchForShare");

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.Navigate(typeof(MainPage), args.ShareOperation);

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        internal static async Task LoginAsync(string token, AsyncEventHandler<DiscordClient, ReadyEventArgs> onReady, Func<Exception, Task> onError, bool background, UserStatus status = UserStatus.Online)
        {
            Exception taskEx = null;

            await _connectSemaphore.WaitAsync();
            try
            {
                var loader = ResourceLoader.GetForViewIndependentUse();

                if (Discord == null)
                {
                    if (background || await WindowsHelloManager.VerifyAsync(VERIFY_LOGIN, loader.GetString("VerifyLoginDisplayReason")))
                    {
                        try
                        {
                            async Task ReadyHandler(DiscordClient sender, ReadyEventArgs e)
                            {
                                LocalSettings.Save("Token", token);

                                sender.Ready -= ReadyHandler;
                                sender.SocketErrored -= SocketErrored;
                                sender.ClientErrored -= ClientErrored;
                                _readySource.TrySetResult(e);
                                if (onReady != null)
                                {
                                    await onReady(sender, e);
                                }
                            }

                            Task SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
                            {
                                sender.Ready -= ReadyHandler;
                                sender.SocketErrored -= SocketErrored;
                                sender.ClientErrored -= ClientErrored;

                                Logger.LogError(e.Exception);

                                _readySource.SetException(e.Exception);
                                return Task.CompletedTask;
                            }

                            Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
                            {
                                sender.Ready -= ReadyHandler;
                                sender.SocketErrored -= SocketErrored;
                                sender.ClientErrored -= ClientErrored;

                                Logger.LogError(e.Exception);

                                _readySource.SetException(e.Exception);
                                return Task.CompletedTask;
                            }

                            Discord = new DiscordClient(new DiscordConfiguration()
                            {
                                Token = token,
                                TokenType = TokenType.User,
                                LoggerFactory = Logger.LoggerFactory
                            });

                            Discord.Ready += ReadyHandler;
                            Discord.SocketErrored += SocketErrored;
                            Discord.ClientErrored += ClientErrored;

                            DiscordClientMessenger.Register(Discord);

                            await Discord.ConnectAsync(status: status, idlesince: AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop" ? (DateTimeOffset?)null : DateTimeOffset.Now);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                            Tools.ResetPasswordVault();
                            _readySource.TrySetException(ex);
                            await onError(ex);
                        }
                    }
                    else
                    {
                        await onError(null);
                    }
                }
                else
                {
                    try
                    {
                        var res = await _readySource.Task;
                        await onReady(App.Discord, res);
                    }
                    catch
                    {
                        await onError(taskEx);
                    }
                }
            }
            finally
            {
                _connectSemaphore.Release();
            }
        }

        internal static async Task LogoutAsync()
        {
            await WebView.ClearTemporaryWebDataAsync();
            await WindowingService.Current.CloseAllWindowsAsync();
            await ImageCache.Instance.ClearAsync();
            await Discord.DisconnectAsync();
            Discord.Dispose();
            Discord = null;

            try
            {
                var passwordVault = new PasswordVault();
                foreach (var c in passwordVault.FindAllByResource(TOKEN_IDENTIFIER))
                {
                    passwordVault.Remove(c);
                }
            }
            catch { }

            DiscordNavigationService.Reset();
            FullscreenService.Reset();
            OverlayService.Reset();
            SettingsService.Reset();
            SwipeOpenService.Reset();

            var frame = (Window.Current.Content as Frame);
            frame.Navigate(typeof(Page));
            frame.BackStack.Clear();
            frame.ForwardStack.Clear();

            frame = new Frame();
            frame.Navigate(typeof(MainPage));
            Window.Current.Content = frame;
        }

        internal static async Task LoginError(Exception ex)
        {
            if (ex != null)
            {
                var loader = ResourceLoader.GetForViewIndependentUse();
                await UIUtilities.ShowErrorDialogAsync(loader.GetString("LoginFailedDialogTitle"), loader.GetString("LoginFailedDialogMessage"));
                RoamingSettings.Save(VERIFY_LOGIN, false);
            }

            Discord = null;

            var mainPage = Window.Current.Content.FindChild<MainPage>();

            if (mainPage.FindChild<LoginPage>() == null)
            {
                mainPage.Nagivate(typeof(LoginPage));
            }

            mainPage.HideConnectingOverlay();
        }
    }
}
