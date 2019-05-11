using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Push;
using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Integration;
using Unicord.Universal.Misc;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Unicord.Universal.Themes;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using static Unicord.Constants;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace Unicord.Universal
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        internal static ulong _currentChannelId = 0;

        private static SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource = new TaskCompletionSource<ReadyEventArgs>();

        internal static DiscordClient Discord { get; set; }
        internal static Thickness StatusBarFill { get; set; }

        internal static LocalObjectStorageHelper LocalSettings { get; } = new LocalObjectStorageHelper();
        internal static RoamingObjectStorageHelper RoamingSettings { get; } = new RoamingObjectStorageHelper();
        
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
            UnhandledException += App_UnhandledException;

            if (RoamingSettings.Read(ENABLE_ANALYTICS, true))
            {
                HockeyClient.Current.Configure(HOCKEYAPP_IDENTIFIER);
                AppCenter.Start(APPCENTER_IDENTIFIER, typeof(Push), typeof(Analytics), typeof(Crashes));
            }
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log(e.Exception);
            e.Handled = true;
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            Resources.MergedDictionaries.Add(new Templates());

            switch (e)
            {
                case ContactPanelActivatedEventArgs cont:
                    await OnContactPanelActivated(cont);
                    return;
                case ToastNotificationActivatedEventArgs toast:
                    await OnLaunchedAsync(false, toast.Argument);
                    return;
                case ProtocolActivatedEventArgs protocol:
                    await OnProtocolActivatedAsync(protocol);
                    return;
                default:
                    Debug.WriteLine(e.Kind);
                    break;
            }
        }

        private static async Task OnProtocolActivatedAsync(ProtocolActivatedEventArgs protocol)
        {
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

                    rootFrame.Navigate(typeof(MainPage), new MainPageEventArgs() { ChannelId = channel, FullFrame = false, IsUriActivation = true });
                    Window.Current.Activate();
                    return;
                }
            }

            await Launcher.LaunchUriAsync(protocol.Uri, new LauncherOptions() { IgnoreAppUriHandlers = true });
        }

        private static async Task OnContactPanelActivated(ContactPanelActivatedEventArgs task)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                try
                {
                    var id = await Contacts.TryGetChannelIdAsync(task.Contact);
                    if (id != 0)
                    {
                        rootFrame.Navigate(typeof(MainPage), new MainPageEventArgs() { UserId = id, FullFrame = true, IsUriActivation = false });
                    }
                }
                catch
                {
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
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Resources.MergedDictionaries.Add(new Templates());
            await OnLaunchedAsync(e.PrelaunchActivated, e.Arguments);
        }

        private async Task OnLaunchedAsync(bool preLaunch, string arguments)
        {
            Analytics.TrackEvent("Launch");

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

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    TODO: Load state from previously suspended application
                //}

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (args.TryGetValue("channelId", out var id) && ulong.TryParse(id, out var pId))
            {
                rootFrame.Navigate(typeof(MainPage), new MainPageEventArgs() { ChannelId = pId, FullFrame = false }, new SuppressNavigationTransitionInfo());
            }
            else
            {
                if (preLaunch == false)
                {
                    CoreApplication.EnablePrelaunch(true);
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainPage), null);
                    }
                }
                else
                {
                    try
                    {
                        var vault = new PasswordVault();
                        var result = vault.FindAllByResource(TOKEN_IDENTIFIER).FirstOrDefault(t => t.UserName == "Default");

                        if (result != null)
                        {
                            result.RetrievePassword();

                            await LoginAsync(result.Password,
                                r => rootFrame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => rootFrame.Navigate(typeof(MainPage))).AsTask(),
                                ex => Task.CompletedTask,
                                true,
                                UserStatus.Idle);
                        }
                    }
                    catch { }
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
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

            Analytics.TrackEvent("Suspend");

            deferral.Complete();
        }

        internal static async Task LoginAsync(string token, AsyncEventHandler<ReadyEventArgs> onReady, Func<Exception, Task> onError, bool background, UserStatus status = UserStatus.Online)
        {
            Exception taskEx = null;

            await _connectSemaphore.WaitAsync();


            if (Discord == null)
            {
                if (background || await WindowsHello.VerifyAsync(VERIFY_LOGIN, "Verify your identitiy to login to Unicord!"))
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

                            var t = new Task(async () => await Contacts.UpdateContactsListAsync(), TaskCreationOptions.LongRunning);
                            t.Start();
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
                            AutomaticGuildSync = false,
                            LogLevel = DSharpPlus.LogLevel.Debug,
                            MutedStore = new UnicordMutedStore(),
                            ReconnectIndefinitely = true
                        }));

                        Discord.DebugLogger.LogMessageReceived += (o, ee) => Logger.Log(ee.Message, ee.Application);
                        Discord.Ready += ReadyHandler;
                        Discord.SocketErrored += SocketErrored;
                        Discord.ClientErrored += ClientErrored;

                        // here we go bois
                        Discord.UseVoiceNext(new VoiceNextConfiguration() { EnableIncoming = true });

                        _connectSemaphore.Release();

                        await Discord.InitializeAsync();
                        await Discord.ConnectAsync(status: status);
                    }
                    catch (Exception ex)
                    {
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
                _connectSemaphore.Release();

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

        internal static async Task LoginError(Exception ex)
        {
            if (ex != null)
            {
                await UIUtilities.ShowErrorDialogAsync("Unable to login!", "Something went wrong logging you in! Check your details and try again!");
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
