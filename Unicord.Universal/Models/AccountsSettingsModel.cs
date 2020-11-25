using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class AccountsSettingsModel : NotifyPropertyChangeImpl
    {
        public AccountsSettingsModel()
        {
            User = App.Discord?.CurrentUser;
        }

        private DiscordUser _user;

        public DiscordUser User
        {
            get => _user;
            set => OnPropertySet(ref _user, value);
        }

        public bool BackgroundNotifications
        {
            get => App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS, true);
            set => App.LocalSettings.Save(BACKGROUND_NOTIFICATIONS, value);
        }

        public string ServerCountString => App.Discord?.Guilds.Count.ToString("N0") ?? "0";
        public string ChannelsCountString => App.Discord?.Guilds.Values.Sum(c => c.Channels.Count).ToString("N0") ?? "0";
        public string MemberCountString => App.Discord?.Guilds.Values.Sum(c => c.MemberCount).ToString("N0") ?? "0";
        public string FriendCountString => App.Discord?.Relationships.Values.Count(r => r.RelationshipType == DiscordRelationshipType.Friend).ToString("N0") ?? "0";
        public string OpenDMCountString => App.Discord?.PrivateChannels.Count().ToString("N0") ?? "0";
        public string SynchedUserCountString => App.Discord?.UserCache.Count.ToString("N0") ?? "0";
        public string SynchedPresenceCountString => App.Discord?.Presences.Count().ToString("N0") ?? "0";
    }
}
