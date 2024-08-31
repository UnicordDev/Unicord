using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Models.Messages;
using Windows.UI;
using Unicord.Universal.Extensions;
using DSharpPlus;
using System.Diagnostics;

namespace Unicord.Universal.Models.User
{
    public class UserViewModel : ViewModelBase, IEquatable<UserViewModel>, IEquatable<DiscordUser>, ISnowflake
    {
        private ulong _userId;
        private ulong? _guildId;

        internal UserViewModel(DiscordUser user, ulong? guildId, ViewModelBase parent = null)
            : base(parent)
        {
            _userId = user.Id;

            if (user is DiscordMember member)
            {
                _guildId = member.Guild.Id;
            }
            else if (guildId != 0)
            {
                _guildId = guildId;
            }

            WeakReferenceMessenger.Default.Register<UserViewModel, UserUpdateEventArgs>(this, (t, e) => t.OnUserUpdate(e.Event));
            WeakReferenceMessenger.Default.Register<UserViewModel, PresenceUpdateEventArgs>(this, (t, e) => t.OnPresenceUpdate(e.Event));

            if (_guildId != null)
            {
                WeakReferenceMessenger.Default.Register<UserViewModel, GuildMemberUpdateEventArgs>(this,
                    (t, e) => t.OnGuildMemberUpdate(e.Event));
                // TODO: idk if this should be here?
                WeakReferenceMessenger.Default.Register<UserViewModel, GuildMembersChunkEventArgs>(this,
                    (t, e) => t.OnGuildMemberChunk(e.Event));
            }
        }

        public ulong Id
            => _userId;

        public DiscordUser User
            => discord.TryGetCachedUser(Id, out var user) ? user : throw new InvalidOperationException();

        public DiscordMember Member
        {
            get
            {
                if (_guildId == null) return null;

                if (!discord.TryGetCachedGuild(_guildId.Value, out var guild))
                    throw new InvalidOperationException();

                return guild.Members.TryGetValue(Id, out var member) ? member : null;
            }
        }

        public string DisplayName
            => Member != null && !string.IsNullOrWhiteSpace(Member.Nickname) ?
            Member.Nickname
            : (User.GlobalName ?? User.Username);

        public string AvatarUrl
            => (Member as DiscordUser)?.GetAvatarUrl(64) ?? User.GetAvatarUrl(64);

        public string Mention
            => User.Mention;

        public bool IsCurrent
            => User.IsCurrent;

        public bool IsBot
            => User.IsBot;

        public DiscordColor Color
            => Member?.Color ?? default;

        public DiscordPresence Presence
            => User.Presence;

        private void OnGuildMemberUpdate(GuildMemberUpdateEventArgs e)
        {
            var member = Member;
            if (member == null || e.Member.Id != member.Id || e.Guild.Id != member.Guild.Id)
                return;

            InvokePropertyChanged(nameof(DisplayName));

            if (!e.RolesBefore.SequenceEqual(e.RolesAfter))
                InvokePropertyChanged(nameof(Color));
        }

        private void OnGuildMemberChunk(GuildMembersChunkEventArgs e)
        {
            if (Member == null || e.Guild.Id != Member.Guild.Id)
                return;

            if (e.Members.TryGetValue(Member.Id, out var member))
            {
                InvokePropertyChanged(nameof(DisplayName));
                InvokePropertyChanged(nameof(Color));
            }
        }

        private void OnUserUpdate(UserUpdateEventArgs e)
        {
            if (User == null || e.UserAfter.Id != User.Id)
                return;

            //_user = e.UserAfter;

            InvokePropertyChanged(nameof(DisplayName));

            if (e.UserAfter.AvatarHash != e.UserBefore.AvatarHash)
                InvokePropertyChanged(nameof(AvatarUrl));
        }

        private void OnPresenceUpdate(PresenceUpdateEventArgs e)
        {
            if (User == null && e.User.Id != User.Id)
                return;

            InvokePropertyChanged(nameof(Presence));
        }

        public bool Equals(UserViewModel other)
        {
            return Equals(other.User);
        }

        public bool Equals(DiscordUser other)
        {
            return ((IEquatable<DiscordUser>)User).Equals(other);
        }
    }
}
