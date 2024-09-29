﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using Unicord.Universal.Pages.Settings;
using Windows.ApplicationModel.Resources;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class AccountsSettingsModel : ViewModelBase
    {
        public AccountsSettingsModel()
        {
            User = discord?.CurrentUser;

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

        public bool BackgroundNotifications
        {
            get => App.LocalSettings.Read(BACKGROUND_NOTIFICATIONS, true);
            set => App.LocalSettings.Save(BACKGROUND_NOTIFICATIONS, value);
        }

        public string ServerCountString => _serverCount == null ? _loading : $"{_serverCount:N0}";
        public string ChannelsCountString => _channelCount == null ? _loading : $"{_channelCount:N0}";
        public string MemberCountString => _memberCount == null ? _loading : $"{_memberCount:N0}";
        public string FriendCountString => _friendCount == null ? _loading : $"{_friendCount:N0}";
        public string OpenDMCountString => _openDMCount == null ? _loading : $"{_openDMCount:N0}";
        public string SynchedUserCountString => _synchedUserCount == null ? _loading : $"{_synchedUserCount:N0}";
        public string SynchedPresenceCountString => _synchedPresenceCount == null ? _loading : $"{_synchedPresenceCount:N0}";
        public string EmoteCountString => _emoteCount == null ? _loading : $"{_emoteCount:N0}";
    }
}
