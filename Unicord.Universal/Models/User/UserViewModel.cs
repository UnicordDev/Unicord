using System;
using System.Linq;
using System.Windows.Input;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Commands;
using Unicord.Universal.Commands.Members;
using Unicord.Universal.Commands.Users;
using Unicord.Universal.Extensions;
using Windows.UI.Xaml;

namespace Unicord.Universal.Models.User
{
    public class UserViewModel : ViewModelBase, IEquatable<UserViewModel>, IEquatable<DiscordUser>, ISnowflake
    {
        protected ulong id;
        protected ulong? guildId;

        private PresenceViewModel _presenceVmCache;


        internal UserViewModel(DiscordUser user, ulong? guildId, ViewModelBase parent = null)
            : this(user.Id, (user as DiscordMember)?.Guild.Id, parent)
        {

        }

        internal UserViewModel(ulong user, ulong? guildId, ViewModelBase parent = null)
            : base(parent)
        {
            id = user;
            this.guildId = guildId;
            WeakReferenceMessenger.Default.Register<UserViewModel, UserUpdateEventArgs>(this,
                (t, e) => t.OnUserUpdate(e.Event));
            WeakReferenceMessenger.Default.Register<UserViewModel, PresenceUpdateEventArgs>(this,
                (t, e) => t.OnPresenceUpdate(e.Event));

            OpenOverlayCommand = new ShowUserOverlayCommand(this);
            MessageCommand = new SendMessageCommand(this);

            if (this.guildId != null)
            {
                // TODO: idk if this should be here?
                WeakReferenceMessenger.Default.Register<UserViewModel, GuildMemberUpdateEventArgs>(this,
                    (t, e) => t.OnGuildMemberUpdate(e.Event));
                WeakReferenceMessenger.Default.Register<UserViewModel, GuildMembersChunkEventArgs>(this,
                    (t, e) => t.OnGuildMemberChunk(e.Event));

                KickCommand = new KickCommand(this);
                BanCommand = new BanCommand(this);
                ChangeNicknameCommand = new ChangeNicknameCommand(this);
            }
        }

        public ulong Id
            => id;

        public DiscordUser User
            => discord.TryGetCachedUser(Id, out var user) ? user : throw new InvalidOperationException();

        public DiscordMember Member
        {
            get
            {
                if (guildId == null) return null;

                if (!discord.TryGetCachedGuild(guildId.Value, out var guild))
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
            => User.Id == discord.CurrentUser.Id;

        public bool IsBot
            => User.IsBot;

        public DiscordColor Color
            => Member?.Color ?? default;

        public PresenceViewModel Presence
            => _presenceVmCache ??= new(User, this);

        public ICommand KickCommand { get; }
        public ICommand BanCommand { get; }
        public ICommand ChangeNicknameCommand { get; }
        public ICommand MessageCommand { get; }
        public ICommand OpenOverlayCommand { get; }

        public Visibility KickVisibility
            => KickCommand?.CanExecute(null) == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BanVisibility
            => BanCommand?.CanExecute(null) == true ? Visibility.Visible : Visibility.Collapsed;

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

            InvokePropertyChanged(nameof(DisplayName));

            if (e.UserAfter.AvatarHash != e.UserBefore.AvatarHash)
                InvokePropertyChanged(nameof(AvatarUrl));
        }

        private void OnPresenceUpdate(PresenceUpdateEventArgs e)
        {
            if (User == null || e.User.Id != User.Id)
                return;

            _presenceVmCache?.OnPresenceUpdated();
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
