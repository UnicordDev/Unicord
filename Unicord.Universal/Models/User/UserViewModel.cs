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

namespace Unicord.Universal.Models.User
{
    public class UserViewModel : ViewModelBase, IEquatable<UserViewModel>, IEquatable<DiscordUser>, ISnowflake
    {
        private readonly DiscordUser _user;
        private readonly DiscordMember _member;

        internal UserViewModel(DiscordUser user, ViewModelBase parent = null)
            : base(parent)
        {
            _user = user;
            _member = user as DiscordMember;

            WeakReferenceMessenger.Default.Register<UserViewModel, UserUpdateEventArgs>(this, (t, e) => t.OnUserUpdate(e.Event));
            WeakReferenceMessenger.Default.Register<UserViewModel, PresenceUpdateEventArgs>(this, (t, e) => t.OnPresenceUpdate(e.Event));

            if (_member != null)
            {
                WeakReferenceMessenger.Default.Register<UserViewModel, GuildMemberUpdateEventArgs>(this,
                    (t, e) => t.OnGuildMemberUpdate(e.Event));
            }
        }

        public ulong Id
            => _user.Id;

        public DiscordUser User
            => _user;

        public DiscordMember Member
            => _member;

        public string DisplayName
            => _member?.DisplayName ?? (_user.GlobalName ?? _user.Username);

        public string AvatarUrl
            => (_member as DiscordUser)?.GetAvatarUrl(64) ?? _user.GetAvatarUrl(64);

        public string Mention
            => _user.Mention;

        public bool IsCurrent
            => _user.IsCurrent;

        public bool IsBot
            => _user.IsBot;

        public DiscordColor Color
            => _user.Color;

        public DiscordPresence Presence
            => _user.Presence;

        private void OnGuildMemberUpdate(GuildMemberUpdateEventArgs e)
        {
            if (_member == null || e.Member.Id != _member.Id || e.Guild.Id != _member.Guild.Id)
                return;

            InvokePropertyChanged(nameof(DisplayName));

            if (!e.RolesBefore.SequenceEqual(e.RolesAfter))
                InvokePropertyChanged(nameof(Color));
        }

        private void OnUserUpdate(UserUpdateEventArgs e)
        {
            if (_user == null || e.UserAfter.Id != _user.Id)
                return;

            InvokePropertyChanged(nameof(DisplayName));

            if (e.UserAfter.AvatarHash != e.UserBefore.AvatarHash)
                InvokePropertyChanged(nameof(AvatarUrl));
        }

        private void OnPresenceUpdate(PresenceUpdateEventArgs e)
        {
            if (_user == null && e.User.Id != _user.Id)
                return;

            InvokePropertyChanged(nameof(Presence));
        }

        public bool Equals(UserViewModel other)
        {
            return Equals(other.User);
        }

        public bool Equals(DiscordUser other)
        {
            return ((IEquatable<DiscordUser>)_user).Equals(other);
        }
    }
}
