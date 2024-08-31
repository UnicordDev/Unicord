using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Shared;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using static Unicord.Constants;

namespace Unicord.Universal.Services
{
    class BackgroundNotificationService : BaseService<BackgroundNotificationService>
    {
        private BadgeManager _badgeManager;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;

        internal async Task StartupAsync()
        {
            if (!await StartBackgroundTaskAsync())
            {
                await StartInProcTaskAsync();
            }
        }

        private async Task RegisterBackgroundTaskAsync()
        {
            try
            {
                if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(TOAST_BACKGROUND_TASK_NAME)))
                    return;

                var status = await BackgroundExecutionManager.RequestAccessAsync();
                var builder = new BackgroundTaskBuilder() { Name = TOAST_BACKGROUND_TASK_NAME };
                builder.SetTrigger(new ToastNotificationActionTrigger());

                var registration = builder.Register();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private async Task<bool> StartBackgroundTaskAsync()
        {
            if (!App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS, true))
                return false;

            await RegisterBackgroundTaskAsync();

            try
            {
                if (ApiInformation.IsApiContractPresent(typeof(StartupTaskContract).FullName, 1))
                {
                    var notifyTask = await StartupTask.GetAsync("UnicordBackgroundTask");
                    await notifyTask.RequestEnableAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            try
            {
                if (ApiInformation.IsApiContractPresent(typeof(FullTrustAppContract).FullName, 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return false;
        }

        private async Task StartInProcTaskAsync()
        {
            _badgeManager = new BadgeManager(App.Discord);
            _tileManager = new TileManager(App.Discord);
            _secondaryTileManager = new SecondaryTileManager(App.Discord);

            await _tileManager.InitialiseAsync();
            await _secondaryTileManager.InitialiseAsync();

            //App.Discord.MessageAcknowledged += OnMessageAcknowledged;
            //App.Discord.MessageCreated += OnMessageCreated;

            WeakReferenceMessenger.Default.Register<BackgroundNotificationService, MessageAcknowledgeEventArgs>(this, (r, e) => r.OnMessageAcknowledged(e.Event));
            WeakReferenceMessenger.Default.Register<BackgroundNotificationService, MessageCreateEventArgs>(this, (r, e) => r.OnMessageCreated(e.Event));
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(e.Message))
                {
                    _badgeManager.Update();
                    await _tileManager.HandleMessageAsync(e.Message);
                }

                await _secondaryTileManager.HandleMessageAsync(e.Message);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        private async Task OnMessageAcknowledged(MessageAcknowledgeEventArgs e)
        {
            try
            {
                _badgeManager.Update();
                await _tileManager.HandleAcknowledgeAsync(e.Channel);
                await _secondaryTileManager.HandleAcknowledgeAsync(e.Channel);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }
    }
}
