using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

#if XBOX_GAME_BAR
using Microsoft.Gaming.XboxGameBar;
using Unicord.Universal.Pages.GameBar;
#endif
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using Unicord.Universal.Dialogs;
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
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.System.Profile;
using Windows.UI.Core;
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

        internal static ApplicationDataStorageHelper LocalSettings { get; }
            = ApplicationDataStorageHelper.GetCurrent();
        internal static ApplicationDataStorageHelper RoamingSettings { get; }
            = ApplicationDataStorageHelper.GetCurrent();

        public App()
        {
            InitializeComponent();

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
            Resuming += OnResuming;
            UnhandledException += OnUnhandledException;

            Debug.WriteLine("Welcome to Unicord!");
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
                    if (args.TaskInstance.TriggerDetails is not ToastNotificationActionTriggerDetail details || !DiscordManager.TryGetToken(out var token))
                        break;

                    var arguments = ParseArgs(details.Argument);
                    var userInput = details.UserInput;

                    if (!arguments.TryGetValue("channelId", out var cId) || !ulong.TryParse(cId, out var channelId))
                        break;

                    if (!userInput.TryGetValue("tbReply", out var t) || t is not string text)
                        break;

                    var client = new DiscordRestClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.User });
                    await client.CreateMessageAsync(channelId, new DiscordMessageBuilder().WithContent(text));
                    break;
            }

            deferral.Complete();
        }
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

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (Window.Current.Content is not Frame rootFrame)
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

        private DiscordClient temporaryCache;

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            if (DiscordManager.Discord != null)
            {
                temporaryCache = DiscordManager.Discord;
                await DiscordManager.Discord.DisconnectAsync(4002);
            }

            await Logger.OnSuspendingAsync();
            deferral.Complete();
        }

        private async void OnResuming(object sender, object e)
        {
            if (temporaryCache != null)
            {
                await temporaryCache.ConnectAsync();
            }
        }

        internal static async Task LogoutAsync()
        {
            await WebView.ClearTemporaryWebDataAsync();
            await WindowingService.Current.CloseAllWindowsAsync();
            await ImageCache.Instance.ClearAsync();
            await DiscordManager.LogoutAsync();

            try
            {
                var passwordVault = new PasswordVault();
                foreach (var c in passwordVault.FindAllByResource(TOKEN_IDENTIFIER))
                {
                    passwordVault.Remove(c);
                }
            }
            catch { }

            // ditto above about the background process
            LocalSettings.TryDelete("Token");

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

            await DiscordManager.LogoutAsync();

            var mainPage = Window.Current.Content.FindChild<MainPage>();

            if (mainPage.FindChild<LoginPage>() == null)
            {
                mainPage.Nagivate(typeof(LoginPage));
            }

            mainPage.HideConnectingOverlay();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);

            if (RoamingSettings.Read(ENABLE_ANALYTICS, true) && APPCENTER_IDENTIFIER != null)
            {
                AppCenter.Start(APPCENTER_IDENTIFIER, typeof(Analytics));
            }
        }
    }
}
