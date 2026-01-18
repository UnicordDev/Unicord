using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json.Bson;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Models.User;
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public partial class AccountsSettingsModel : ViewModelBase
    {
        private static readonly bool _isWindows11 = SystemInformation.Instance.OperatingSystemVersion.Build >= 22000;
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse("AccountsSettingsPage");

        public AccountsSettingsModel()
        {
            User = discord?.CurrentUser;

            if (User != null)
            {
                UserPresence = new PresenceViewModel(User, this);
                WeakReferenceMessenger.Default.Register<AccountsSettingsModel, DiscordEventMessage<PresenceUpdateEventArgs>>(this,
                    (t, e) => t.OnPresenceUpdate(e.Event));
            }

            var strings = ResourceLoader.GetForCurrentView(nameof(AccountsSettingsPage));
            _loading = strings.GetString("Loading");
            _ = Task.Run(() =>
             {
                 if (discord == null)
                     return;

                 _serverCount = discord.Guilds.Count; 
                 InvokePropertyChanged(nameof(ServerCountString));

                 _channelCount = discord.Guilds.Values.Sum(c => c.Channels.Count) + discord.PrivateChannels.Count;
                 InvokePropertyChanged(nameof(ChannelsCountString));

                 _memberCount = discord.Guilds.Values.Sum(c => c.MemberCount);
                 InvokePropertyChanged(nameof(MemberCountString));

                 _friendCount = discord.Relationships.Values.Count(r => r.RelationshipType == DiscordRelationshipType.Friend); 
                 InvokePropertyChanged(nameof(FriendCountString));

                 _openDMCount = discord.PrivateChannels.Count;
                 InvokePropertyChanged(nameof(OpenDMCountString));

                 _synchedUserCount = discord.UserCacheCount;
                 InvokePropertyChanged(nameof(SynchedUserCountString));

                 _synchedPresenceCount = discord.Presences.Count;
                 InvokePropertyChanged(nameof(SynchedPresenceCountString));

                 _emoteCount = discord.Guilds.Values.Sum(c => c.Emojis.Count);
                 InvokePropertyChanged(nameof(EmoteCountString));
             });
        }

        private DiscordUser _user;
        private string _loading;

        public PresenceViewModel UserPresence { get; }

        private int? _serverCount;
        private int? _channelCount;
        private int? _memberCount;
        private int? _friendCount;
        private int? _openDMCount;
        private int? _synchedUserCount;
        private int? _synchedPresenceCount;
        private int? _emoteCount;

        public DiscordUser User
        {
            get => _user;
            set => OnPropertySet(ref _user, value);
        }

        private void OnPresenceUpdate(PresenceUpdateEventArgs e)
        {
            if (User == null || UserPresence == null || e.User.Id != User.Id)
                return;

            UserPresence.OnPresenceUpdated();
        }

        public bool BackgroundNotifications
        {
            get => App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS_FULL_TRUST, true);
            set => App.LocalSettings.Save(BACKGROUND_NOTIFICATIONS_FULL_TRUST, value);
        }

        public string BackgroundNotificationIcon
            => _isWindows11 ? "\uEA8F" : "\uE7E7";

        public string BackgroundNotificationDescription
            => _isWindows11
                ? _resourceLoader.GetString("BackgroundNotificationsDescriptionWin11")
                : _resourceLoader.GetString("BackgroundNotificationsDescriptionWin10");

        public bool IsSyncContactsVisible
            => !_isWindows11;

        public string ServerCountString => _serverCount == null ? _loading : $"{_serverCount:N0}";
        public string ChannelsCountString => _channelCount == null ? _loading : $"{_channelCount:N0}";
        public string MemberCountString => _memberCount == null ? _loading : $"{_memberCount:N0}";
        public string FriendCountString => _friendCount == null ? _loading : $"{_friendCount:N0}";
        public string OpenDMCountString => _openDMCount == null ? _loading : $"{_openDMCount:N0}";
        public string SynchedUserCountString => _synchedUserCount == null ? _loading : $"{_synchedUserCount:N0}";
        public string SynchedPresenceCountString => _synchedPresenceCount == null ? _loading : $"{_synchedPresenceCount:N0}";
        public string EmoteCountString => _emoteCount == null ? _loading : $"{_emoteCount:N0}";

        [RelayCommand]
        private void CopyStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Friends\t\t\t{FriendCountString}");
            sb.AppendLine($"Servers\t\t\t{ServerCountString}");
            sb.AppendLine($"Server Channels\t\t{ChannelsCountString}");
            sb.AppendLine($"Server Members\t\t{MemberCountString}");
            sb.AppendLine($"DM Channels\t\t{OpenDMCountString}");
            sb.AppendLine($"Synced Users\t\t{SynchedUserCountString}");
            sb.AppendLine($"Synced Presences\t{SynchedPresenceCountString}");
            sb.AppendLine($"Emotes\t\t\t{EmoteCountString}");

            var dataPackage = new DataPackage();
            dataPackage.SetText(sb.ToString());
            Clipboard.SetContent(dataPackage);
        }

        [RelayCommand]
        private void CopyUserId()
        { 
            var userId = DiscordManager.Discord.CurrentUser?.Id;
            if (userId == null) return;

            var dataPackage = new DataPackage();
            dataPackage.SetText(userId.Value.ToString());
            Clipboard.SetContent(dataPackage);
        }
    }
}
