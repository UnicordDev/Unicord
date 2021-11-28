using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Channel;
using Unicord.Universal.Models.Voice;

namespace Unicord.Universal.Models
{
    // TODO: Move functionality from DiscordPage.xaml.cs into this class
    class DiscordPageModel : NotifyPropertyChangeImpl
    {
        private readonly SynchronizationContext _synchronisation;
        private VoiceConnectionModel _voiceModel;
        private DiscordUser _currentUser;
        private DiscordChannel _currentChannel;
        private DiscordDmChannel _selectedDM;
        private GuildListViewModel _selectedGuild;
        private bool _isFriendsSelected;
        private bool _isRightPaneOpen;

        public DiscordPageModel()
        {
            _synchronisation = SynchronizationContext.Current;

            Guilds = new ObservableCollection<IGuildListViewModel>();
            UnreadDMs = new ObservableCollection<DiscordDmChannel>();
            CurrentUser = App.Discord.CurrentUser;

            var guilds = App.Discord.Guilds;
            var folders = App.Discord.UserSettings?.GuildFolders;
            var ids = new HashSet<ulong>();
            if (folders != null)
            {
                foreach (var folder in folders)
                {
                    if (folder.Id == null || folder.Id == 0)
                    {
                        foreach (var id in folder.GuildIds)
                        {
                            if (guilds.TryGetValue(id, out var server))
                            {
                                Guilds.Add(new GuildListViewModel(server));
                                ids.Add(id);
                            }
                        }

                        continue;
                    }

                    var folderItems = folder.GuildIds.Select(id => guilds.TryGetValue(id, out var server) ? server : null)
                                                     .Where(g => g != null);

                    foreach (var item in folderItems)
                        ids.Add(item.Id);

                    Guilds.Add(new GuildListFolderViewModel(folder, folderItems));
                }
            }

            foreach (var guild in App.Discord.Guilds.Values)
            {
                if (!ids.Contains(guild.Id))
                    Guilds.Insert(0, new GuildListViewModel(guild));
            }

            var dms = App.Discord.PrivateChannels.Values;
            foreach (var dm in dms.Where(d => d.ReadState?.MentionCount > 0).OrderByDescending(d => d.ReadState?.LastMessageId))
            {
                UnreadDMs.Add(dm);
            }

            App.Discord.GuildCreated += OnGuildCreated;
            App.Discord.GuildDeleted += OnGuildDeleted;
            App.Discord.MessageCreated += OnMessageCreated;
            App.Discord.MessageAcknowledged += OnMessageAcknowledged;
            App.Discord.UserSettingsUpdated -= OnUserSettingsUpdated;
        }

        public bool Navigating { get; internal set; }
        public ObservableCollection<IGuildListViewModel> Guilds { get; }
        public ObservableCollection<DiscordDmChannel> UnreadDMs { get; }

        public DiscordUser CurrentUser { get => _currentUser; set => OnPropertySet(ref _currentUser, value); }
        public VoiceConnectionModel VoiceModel { get => _voiceModel; set => OnPropertySet(ref _voiceModel, value); }

        public DiscordChannel CurrentChannel { get => _currentChannel; set => OnPropertySet(ref _currentChannel, value); }
        public DiscordDmChannel SelectedDM { get => _selectedDM; set => OnPropertySet(ref _selectedDM, value); }
        public GuildListViewModel SelectedGuild
        {
            get => _selectedGuild;
            set => OnPropertySet(ref _selectedGuild, value);

        }
        public bool IsFriendsSelected { get => _isFriendsSelected; set => OnPropertySet(ref _isFriendsSelected, value); }
        public bool IsRightPaneOpen { get => _isRightPaneOpen; set => OnPropertySet(ref _isRightPaneOpen, value); }
        public DiscordDmChannel PreviousDM { get; set; }

        public GuildListViewModel ViewModelFromGuild(DiscordGuild guild)
        {
            foreach (var guildVM in Guilds)
            {
                if (guildVM.TryGetModelForGuild(guild, out var model))
                    return model;
            }

            return null;
        }

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
            var vm = this.ViewModelFromGuild(e.Guild);
            if (vm == null)
            {
                _synchronisation.Post(d => Guilds.Insert(0, new GuildListViewModel(e.Guild)), null);
            }

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            var vm = this.ViewModelFromGuild(e.Guild);
            if (vm == null)
            {
                _synchronisation.Post(d =>
                {
                    Guilds.Remove(vm);
                    foreach (var guildFolder in Guilds.OfType<GuildListFolderViewModel>())
                        guildFolder.Children.Remove(vm);
                }, null);
            }
            return Task.CompletedTask;
        }

        private Task OnUserSettingsUpdated(UserSettingsUpdateEventArgs e)
        {
            //var guildPositions = App.Discord.UserSettings?.GuildPositions;
            //if (guildPositions == null || Guilds.Select(g => g.Id).SequenceEqual(guildPositions))
            //    return Task.CompletedTask;

            //for (var i = 0; i < guildPositions.Count; i++)
            //{
            //    var id = guildPositions[i];
            //    var guild = Guilds[i];
            //    if (id != guild.Id)
            //    {
            //        _synchronisation.Post((o) => Guilds.Move(Guilds.IndexOf(Guilds.First(g => g.Id == id)), i), null);
            //    }
            //}

            return Task.CompletedTask;
        }
    }
}
