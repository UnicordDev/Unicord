using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Push;

using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Abstractions;
using Unicord.Universal.Abstractions;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.System.Threading;
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

        internal static TaskCompletionSource<ReadyEventArgs> ReadySource { get; } = new TaskCompletionSource<ReadyEventArgs>();

        internal static DiscordClient Discord { get; set; }
        internal static Thickness StatusBarFill { get; set; }

        internal static RoamingObjectStorageHelper RoamingSettings { get; private set; } = new RoamingObjectStorageHelper();
        internal static ConcurrentDictionary<ulong, DiscordRestClient> AdditionalUserClients { get; private set; } = new ConcurrentDictionary<ulong, DiscordRestClient>();
        internal static UwpMediaAbstractions MediaAbstractions { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
            UnhandledException += App_UnhandledException;

            MediaAbstractions = new UwpMediaAbstractions();
            UIAbstractions.SetAbstractions<UwpUIAbstractions>();

            HockeyClient.Current.Configure(HOCKEYAPP_IDENTIFIER);
            AppCenter.Start(APPCENTER_IDENTIFIER, typeof(Push));
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
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
                    await OnLaunchedAsync(false, toast.Argument);
                    return;
                default:
                    Debug.WriteLine(e.Kind);
                    break;
            }
        }

        private static async Task OnContactPanelActivated(ContactPanelActivatedEventArgs task)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                try
                {
                    var contacts = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                    var manager = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                    var contact = await contacts.GetContactAsync(task.Contact.Id);
                    var annotations = await manager.FindAnnotationsForContactAsync(contact);
                    var annotation = annotations.FirstOrDefault();

                    if (ulong.TryParse(annotation.RemoteId.Split('_').Last(), out var id))
                    {
                        rootFrame.Navigate(typeof(MainPage), new MainPageEventArgs() { UserId = id, FullFrame = true });
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
            await OnLaunchedAsync(e.PrelaunchActivated, e.Arguments);
        }

        private async Task OnLaunchedAsync(bool preLaunch, string arguments)
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
                                async r => await rootFrame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => rootFrame.Navigate(typeof(MainPage))),
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


            deferral.Complete();
        }

        internal static async Task LoginAsync(string token, AsyncEventHandler<ReadyEventArgs> onReady, Func<Exception, Task> onError, bool background, UserStatus status = UserStatus.Online)
        {
            Exception taskEx = null;
            //try
            //{
            //    var localStorage = new LocalObjectStorageHelper();
            //    if (!localStorage.KeyExists("background-allowed"))
            //    {
            //        await BackgroundExecutionManager.RequestAccessAsync();
            //        var val = await BackgroundExecutionManager.RequestAccessKindAsync(
            //            BackgroundAccessRequestKind.AllowedSubjectToSystemPolicy,
            //            "Unicord can run in the background to keep you notified about DMs and incomming calls!");
            //        localStorage.Save("background-allowed", val);
            //    }
            //}
            //catch { }

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
                            ReadySource.TrySetResult(e);
                            if (onReady != null)
                                await onReady(e);

                            if (RoamingSettings.Read(SYNC_CONTACTS, true) && ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                            {
                                var t = new Task(async () => await Contacts.UpdateContactsListAsync(), TaskCreationOptions.LongRunning);
                                t.Start();
                            }
                        }

                        Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.User, AutomaticGuildSync = false, LogLevel = DSharpPlus.LogLevel.Debug });
                        Discord.DebugLogger.LogMessageReceived += (o, ee) => Logger.Log(ee.Message, ee.Application);
                        Discord.Ready += ReadyHandler;

                        _connectSemaphore.Release();

                        await Discord.ConnectAsync(status: status);
                    }
                    catch (Exception ex)
                    {
                        Tools.ResetPasswordVault();
                        ReadySource.TrySetException(ex);
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
                    var res = await ReadySource.Task;
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
                var dialog = new ErrorDialog()
                {
                    Title = "Unable to login!",
                    Text = "Something went wrong logging you in! Check your details and try again!",
                    AdditionalText = ex.Message
                };
                await dialog.ShowAsync();

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
