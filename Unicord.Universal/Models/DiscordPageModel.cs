using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Voice;

namespace Unicord.Universal.Models
{
    // TODO: Move functionaliy from DiscordPage.xaml.cs into this class
    class DiscordPageModel : PropertyChangedBase
    {
        private readonly SynchronizationContext _synchronisation;
        private VoiceConnectionModel _voiceModel;
        private DiscordUser _currentUser;
        private DiscordChannel _currentChannel;
        private DiscordDmChannel _selectedDM;
        private DiscordGuild _selectedGuild;
        private bool _isFriendsSelected;

        public DiscordPageModel()
        {
            _synchronisation = SynchronizationContext.Current;

            Guilds = new ObservableCollection<DiscordGuild>();
            UnreadDMs = new ObservableCollection<DiscordDmChannel>();
            CurrentUser = App.Discord.CurrentUser;

            var guildPositions = App.Discord.UserSettings?.GuildPositions;
            foreach (var guild in App.Discord.Guilds.Values.OrderBy(g => guildPositions?.IndexOf(g.Id) ?? 0))
            {
                Guilds.Add(guild);
            }

            foreach (var dm in App.Discord.PrivateChannels.Values)
            {
                if (dm.ReadState.MentionCount > 0)
                {
                    UnreadDMs.Add(dm);
                }
            }

            App.Discord.MessageCreated += OnMessageCreated;
            App.Discord.MessageAcknowledged += OnMessageAcknowledged;
            App.Discord.GuildCreated += OnGuildCreated;
            App.Discord.GuildDeleted += OnGuildDeleted;
            App.Discord.UserSettingsUpdated -= OnUserSettingsUpdated;
        }

        public ObservableCollection<DiscordGuild> Guilds { get; }
        public ObservableCollection<DiscordDmChannel> UnreadDMs { get; }

        public DiscordUser CurrentUser { get => _currentUser; set => OnPropertySet(ref _currentUser, value); }
        public VoiceConnectionModel VoiceModel { get => _voiceModel; set => OnPropertySet(ref _voiceModel, value); }
        public bool Navigating { get; internal set; }

        public DiscordChannel CurrentChannel { get => _currentChannel; internal set => OnPropertySet(ref _currentChannel, value); }
        public DiscordDmChannel SelectedDM { get => _selectedDM; internal set => OnPropertySet(ref _selectedDM, value); }
        public DiscordGuild SelectedGuild { get => _selectedGuild; internal set => OnPropertySet(ref _selectedGuild, value); }
        public bool IsFriendsSelected { get => _isFriendsSelected; internal set => OnPropertySet(ref _isFriendsSelected, value); }

        private Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                UpdateReadState(dm);
            }

            return Task.CompletedTask;
        }

        private Task OnMessageAcknowledged(MessageAcknowledgeEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                UpdateReadState(dm);
            }

            return Task.CompletedTask;
        }

        private void UpdateReadState(DiscordDmChannel dm)
        {
            if (dm.ReadState?.MentionCount > 0 && !UnreadDMs.Contains(dm))
            {
                _synchronisation.Post((o) => UnreadDMs.Insert(0, dm), null);
            }
            else if (dm.ReadState?.MentionCount == 0)
            {
                _synchronisation.Post((o) => UnreadDMs.Remove(dm), null);
            }
        }

        private Task OnGuildCreated(GuildCreateEventArgs e)
        {
            if (!Guilds.Contains(e.Guild))
            {
                _synchronisation.Post(d => Guilds.Insert(0, e.Guild), null);
            }

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            _synchronisation.Post(d => Guilds.Remove(e.Guild), null);
            return Task.CompletedTask;
        }

        private Task OnUserSettingsUpdated(UserSettingsUpdateEventArgs e)
        {
            var guildPositions = App.Discord.UserSettings?.GuildPositions;
            if (!Guilds.Select(g => g.Id).SequenceEqual(guildPositions))
            {
                for (var i = 0; i < guildPositions.Count; i++)
                {
                    var id = guildPositions[i];
                    var guild = Guilds[i];
                    if (id != guild.Id)
                    {
                        _synchronisation.Post((o) => Guilds.Move(Guilds.IndexOf(Guilds.First(g => g.Id == id)), i), null);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
