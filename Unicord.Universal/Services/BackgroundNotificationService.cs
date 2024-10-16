using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Background.Tasks;
using Unicord.Universal.Extensions;
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
        private readonly ILogger<BackgroundNotificationService> _logger 
            = Logger.GetLogger<BackgroundNotificationService>();

        private BadgeManager _badgeManager;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;

        internal async Task StartupAsync()
        {
            if (await StartFullTrustBackgroundTaskAsync())
            {
                
            }
            else
            {
                //await RegisterPeriodicBackgroundTaskAsync();
                await StartInProcTaskAsync();
            }

            var periodicTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(i => i.Name.Equals(PERIODIC_BACKGROUND_TASK_NAME));
            if (periodicTask != null)
            {
                _logger.LogInformation("Disabling periodic background task because full-trust task is running.");
                periodicTask.Unregister(true);
            }
        }

        private async Task<bool> RegisterToastActivationBackgroundTaskAsync()
        {
            try
            {
                if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(TOAST_BACKGROUND_TASK_NAME)))
                    return true;


                var status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status is BackgroundAccessStatus.Denied or BackgroundAccessStatus.DeniedBySystemPolicy or BackgroundAccessStatus.DeniedByUser)
                {
                    _logger.LogError("Failed to register background task for toast notification because {Reason}", status.ToString());
                    return false;
                }

                var builder = new BackgroundTaskBuilder() { Name = TOAST_BACKGROUND_TASK_NAME };
                builder.SetTrigger(new ToastNotificationActionTrigger());
                var registration = builder.Register();

                _logger.LogInformation("Registered background task for toast activation.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register background task for toast activation.");
            }

            return false;
        }

        private async Task<bool> StartFullTrustBackgroundTaskAsync()
        {
            if (!App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS, true))
            {
                _logger.LogDebug("Not starting full-trust notifications process, disabled by user.");
                return false;
            }

            // disable this prior to FCU because it seems to just crash, and inbuilt notifications work better
            if (!ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 5))
            {
                _logger.LogDebug("Not starting full-trust notifications process, disabled by OS version check.");
                return false;
            }

            if (!await RegisterToastActivationBackgroundTaskAsync())
            {
                _logger.LogDebug("Not starting full-trust notifications process, failed to register toast activation task.");
                return false;
            }

            try
            {
                if (ApiInformation.IsApiContractPresent(typeof(FullTrustAppContract).FullName, 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    _logger.LogInformation("Launched full-trust notifications process.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch full-trust notifications process.");
                return false;
            }

            try
            {
                if (ApiInformation.IsApiContractPresent(typeof(StartupTaskContract).FullName, 1))
                {
                    var notifyTask = await StartupTask.GetAsync("UnicordBackgroundTask");
                    var state = await notifyTask.RequestEnableAsync();

                    _logger.LogInformation("Full-trust notifications startup task state -> {State}", state);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable startup task for full-trust notifications process.");
            }

            return true;
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
