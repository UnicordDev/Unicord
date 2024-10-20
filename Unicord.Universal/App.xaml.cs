using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
#if XBOX_GAME_BAR
using Microsoft.Gaming.XboxGameBar;
using Unicord.Universal.Pages.GameBar;
#endif
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Models;
using Unicord.Universal.Services;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            Tools.MigratePreV2Settings();

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

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            switch (args.TaskInstance.Task.Name)
            {
                case TOAST_BACKGROUND_TASK_NAME:
                    if (args.TaskInstance.TriggerDetails is not ToastNotificationActionTriggerDetail details || !LoginService.TryGetToken(out var token))
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

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunched(e.PrelaunchActivated, e.Arguments, e.PreviousExecutionState);
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);

            if (RoamingSettings.Read(ENABLE_ANALYTICS, true) && APPCENTER_IDENTIFIER != null)
            {
                AppCenter.Start(APPCENTER_IDENTIFIER, typeof(Analytics));
            }
        }

        private async void OnLaunched(bool preLaunch, string arguments, ApplicationExecutionState previousState = ApplicationExecutionState.NotRunning)
        {
            var channelId = 0ul;
            var args = ParseArgs(arguments);

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (Window.Current.Content is not Frame rootFrame)
            {
                Analytics.TrackEvent("Unicord_Launch");

                if (previousState == ApplicationExecutionState.Terminated)
                    channelId = LocalSettings.Read("LastViewedChannel", 0ul);

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                WindowingService.Current.SetMainWindow(rootFrame);
                Window.Current.Content = rootFrame;
            }

            if (args.TryGetValue("channelId", out var id) && ulong.TryParse(id, out var pId))
                channelId = pId;

            if (!preLaunch)
            {
                CoreApplication.EnablePrelaunch(true);

                if (rootFrame.Content == null)
                    rootFrame.Navigate(typeof(MainPage));

                if (channelId != 0)
                {
                    await DiscordNavigationService.GetForCurrentView()
                        .NavigateAsync(NavigationType.Channel, channelId, 0);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
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

            GC.Collect(2, GCCollectionMode.Forced, true, true);

            deferral.Complete();
        }

        private async void OnResuming(object sender, object e)
        {
            var discord = Interlocked.Exchange(ref temporaryCache, null);
            if (discord != null)
                await discord.ConnectAsync();
        }


    }
}
