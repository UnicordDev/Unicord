using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Crashes;
using Unicord.Universal.Background.Tasks;
using Unicord.Universal.Shared;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
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
            if (!await StartFullTrustBackgroundTaskAsync())
            {
                await RegisterPeriodicBackgroundTaskAsync();
                await StartInProcTaskAsync();
            }
        }

        private async Task<bool> RegisterBackgroundTaskAsync()
        {
            try
            {
                if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(TOAST_BACKGROUND_TASK_NAME)))
                    return true;

                var status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status is BackgroundAccessStatus.Denied or BackgroundAccessStatus.DeniedBySystemPolicy or BackgroundAccessStatus.DeniedByUser)
                    return false;

                var builder = new BackgroundTaskBuilder() { Name = TOAST_BACKGROUND_TASK_NAME };
                builder.SetTrigger(new ToastNotificationActionTrigger());

                var registration = builder.Register();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return false;
        }

        private async Task<bool> StartFullTrustBackgroundTaskAsync()
        {
            if (!App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS, true))
                return false;

            if (!ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 5))
                return false; // disable this prior to FCU because it seems to just crash, and inbuilt notifications work better

            if (!await RegisterBackgroundTaskAsync())
                return false;

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


        private async Task<bool> RegisterPeriodicBackgroundTaskAsync()
        {
            try
            {
                if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(PERIODIC_BACKGROUND_TASK_NAME)))
                    return true;

                var status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status is BackgroundAccessStatus.Denied or BackgroundAccessStatus.DeniedBySystemPolicy or BackgroundAccessStatus.DeniedByUser)
                    return false;

                var builder = new BackgroundTaskBuilder()
                {
                    Name = PERIODIC_BACKGROUND_TASK_NAME,
                    TaskEntryPoint = typeof(PeriodicNotificationsTask).FullName,
                    IsNetworkRequested = true
                };

                builder.SetTrigger(new TimeTrigger(15, false));

                var registration = builder.Register();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return false;
        }

        private async Task StartInProcTaskAsync()
        {
            _badgeManager = new BadgeManager(DiscordManager.Discord);
            _tileManager = new TileManager(DiscordManager.Discord);
            _secondaryTileManager = new SecondaryTileManager(DiscordManager.Discord);

            await _tileManager.InitialiseAsync();
            await _secondaryTileManager.InitialiseAsync();

            WeakReferenceMessenger.Default.Register<BackgroundNotificationService, MessageAcknowledgeEventArgs>(this,
                (r, e) => r.OnMessageAcknowledged(e.Event));
            WeakReferenceMessenger.Default.Register<BackgroundNotificationService, MessageCreateEventArgs>(this,
                (r, e) => r.OnMessageCreated(e.Event));
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(DiscordManager.Discord, e.Message))
                {
                    _badgeManager.Update();
                    await _tileManager.HandleMessageAsync(e.Message);
                }

                await _secondaryTileManager.HandleMessageAsync(DiscordManager.Discord, e.Message);
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
