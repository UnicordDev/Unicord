using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.WebSocket;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Push;
//using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Integration;
using Unicord.Universal.Misc;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Unicord.Universal.Pages.GameBar;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
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
        //private static XboxGameBarWidget _chatListWidget;
        //private static XboxGameBarWidget _friendsListWidget;

        private static SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource = new TaskCompletionSource<ReadyEventArgs>();

        internal static DiscordClient Discord { get; private set; }
        internal static LocalObjectStorageHelper LocalSettings { get; } = new LocalObjectStorageHelper();
        internal static RoamingObjectStorageHelper RoamingSettings { get; } = new RoamingObjectStorageHelper();

        public App()
        {
            InitializeComponent();

            var provider = VersionHelper.RegisterVersionProvider<UnicordVersionProvider>();
            var theme = LocalSettings.Read(REQUESTED_COLOUR_SCHEME, ElementTheme.Default);
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

            if (RoamingSettings.Read(ENABLE_ANALYTICS, true) && APPCENTER_IDENTIFIER != null)
            {
               AppCenter.Start(APPCENTER_IDENTIFIER, typeof(Push), typeof(Analytics), typeof(Crashes));
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.LogError(e.Exception);
            Logger.Log(e.Message);
            e.Handled = true;
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            try
            {
                ThemeManager.LoadCurrentTheme(Resources);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            switch (e)
            {
                case ContactPanelActivatedEventArgs cont:
                    await OnContactPanelActivated(cont);
                    return;
                case ToastNotificationActivatedEventArgs toast:
                    OnLaunched(false, toast.Argument);
                    return;
                case ProtocolActivatedEventArgs protocol:
                    await OnProtocolActivatedAsync(protocol);
                    return;
                //case XboxGameBarWidgetActivatedEventArgs xbox:
                //    OnXboxGameBarActivated(xbox);
                //    return;
                default:
                    Debug.WriteLine(e.Kind);
                    break;
            }
        }

        //private void OnXboxGameBarActivated(XboxGameBarWidgetActivatedEventArgs xbox)
        //{
        //    if (xbox.IsLaunchActivation)
        //    {
        //        var name = xbox.Uri.LocalPath;
        //        var frame = new Frame();
        //        frame.NavigationFailed += OnNavigationFailed;
        //        Window.Current.Content = frame;

        //        if (name == "unicord-friendslist")
        //        {
        //            _friendsListWidget = new XboxGameBarWidget(xbox, Window.Current.CoreWindow, frame);
        //            Window.Current.Closed += OnFrendsListWidgetClosed;

        //            frame.Navigate(typeof(GameBarMainPage));
        //        }
        //    }
        //}

        //private void OnFrendsListWidgetClosed(object sender, CoreWindowEventArgs e)
        //{
        //    Window.Current.Closed -= OnFrendsListWidgetClosed;
        //    _friendsListWidget = null;
        //}

        private static async Task OnProtocolActivatedAsync(ProtocolActivatedEventArgs protocol)
        {
            Analytics.TrackEvent("Unicord_LaunchForProtocol");

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

            await Launcher.LaunchUriAsync(protocol.Uri, new LauncherOptions() { IgnoreAppUriHandlers = true });
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

            var channelId = 0ul;
            Exception themeLoadException = null;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                UpgradeSettings();

                Analytics.TrackEvent("Unicord_Launch");
                WindowManager.SetMainWindow();

                try
                {
                    ThemeManager.LoadCurrentTheme(Resources);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    themeLoadException = ex;
                }

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousState == ApplicationExecutionState.Terminated)
                {
                    channelId = LocalSettings.Read("LastViewedChannel", 0ul);
                }

                // Place the frame in the current Window
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

        /// <summary>
        /// This method migrates settings from previous versions of Unicord.
        /// </summary>
        private void UpgradeSettings()
        {
            // migrates the old theme store 
#pragma warning disable CS0618 // Type or member is obsolete
            var theme = LocalSettings.Read<string>(SELECTED_THEME_NAME, null);
            if (!string.IsNullOrWhiteSpace(theme))
            {
                LocalSettings.Save(SELECTED_THEME_NAME, (string)null);
                LocalSettings.Save(SELECTED_THEME_NAMES, new List<string>() { theme });
            }

            var themes = LocalSettings.Read<List<string>>(SELECTED_THEME_NAMES);
            if (themes != null)
            {
                themes.RemoveAll(t => string.IsNullOrWhiteSpace(t));
                LocalSettings.Save(SELECTED_THEME_NAMES, themes);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            if (Window.Current.Content != null)
            {
                ApplicationView view = null;
                var coreView = CoreApplication.CreateNewView();
                await coreView.Dispatcher.AwaitableRunAsync(() => view = SetupCurrentView());
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(view.Id, ViewSizePreference.UseMinimum);
                await coreView.Dispatcher.AwaitableRunAsync(() => OnFileActivatedForWindow(args));
            }
            else
            {
                SetupCurrentView();
                OnFileActivatedForWindow(args);
            }
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

        private void OnFileActivatedForWindow(FileActivatedEventArgs args)
        {
            if (Window.Current.Content is Frame f)
            {
                f.Navigate(typeof(InstallThemePage), args);
            }
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                UpgradeSettings();
                Analytics.TrackEvent("Unicord_LaunchForShare");

                try
                {
                    ThemeManager.LoadCurrentTheme(Resources);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }

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

        private async void OnExtendedSessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            if (args.Reason == ExtendedExecutionRevokedReason.SystemPolicy)
            {
                await Discord.DisconnectAsync();
                Discord = null;
            }
        }

        internal static async Task LoginAsync(string token, AsyncEventHandler<ReadyEventArgs> onReady, Func<Exception, Task> onError, bool background, UserStatus status = UserStatus.Online)
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
                            async Task ReadyHandler(ReadyEventArgs e)
                            {
                                e.Client.Ready -= ReadyHandler;
                                e.Client.SocketErrored -= SocketErrored;
                                e.Client.ClientErrored -= ClientErrored;
                                _readySource.TrySetResult(e);
                                if (onReady != null)
                                {
                                    await onReady(e);
                                }
                            }

                            Task SocketErrored(SocketErrorEventArgs e)
                            {
                                e.Client.Ready -= ReadyHandler;
                                e.Client.SocketErrored -= SocketErrored;
                                e.Client.ClientErrored -= ClientErrored;
                                _readySource.SetException(e.Exception);
                                return Task.CompletedTask;
                            }

                            Task ClientErrored(ClientErrorEventArgs e)
                            {
                                e.Client.Ready -= ReadyHandler;
                                e.Client.SocketErrored -= SocketErrored;
                                e.Client.ClientErrored -= ClientErrored;
                                _readySource.SetException(e.Exception);
                                return Task.CompletedTask;
                            }

                            Discord = await Task.Run(() => new DiscordClient(new DiscordConfiguration()
                            {
                                Token = token,
                                TokenType = TokenType.User,
                                LogLevel = DSharpPlus.LogLevel.Debug,
                                GatewayCompressionLevel = GatewayCompressionLevel.None,
                                ReconnectIndefinitely = true
                            }));

                            Discord.DebugLogger.LogMessageReceived += (o, ee) => Logger.Log(ee.Message, ee.Application);
                            Discord.Ready += ReadyHandler;
                            Discord.SocketErrored += SocketErrored;
                            Discord.ClientErrored += ClientErrored;

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
                        await onReady(res);
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
