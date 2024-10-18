using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.Voice;
using Unicord.Universal.Services;
using Windows.ApplicationModel;

namespace Unicord.Universal.Models
{
    // TODO: Move functionality from DiscordPage.xaml.cs into this class
    class DiscordPageViewModel : ViewModelBase
    {
        private VoiceConnectionModel _voiceModel;
        private DiscordUser _currentUser;
        private DiscordChannel _currentChannel;
        private ChannelViewModel _selectedDM;
        private GuildListViewModel _selectedGuild;
        private bool _isFriendsSelected;
        private bool _isRightPaneOpen;

        public DiscordPageViewModel()
        {
            Guilds = [];
            UnreadDMs = [];

            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, ReadyEventArgs>(this, (t, v) => t.OnReady(v.Event));
            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, GuildCreateEventArgs>(this, (t, v) => t.OnGuildCreated(v.Event));
            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, GuildDeleteEventArgs>(this, (t, v) => t.OnGuildDeleted(v.Event));
            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, MessageCreateEventArgs>(this, (t, v) => t.OnMessageCreated(v.Event));
            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, MessageAcknowledgeEventArgs>(this, (t, v) => t.OnMessageAcknowledged(v.Event));
            WeakReferenceMessenger.Default.Register<DiscordPageViewModel, UserSettingsUpdateEventArgs>(this, (t, v) => t.OnUserSettingsUpdated(v.Event));

            Load();
        }

        private void Load()
        {
            if (discord == null) return;

            Guilds.Clear();
            UnreadDMs.Clear();
            CurrentUser = discord.CurrentUser;

            var guilds = discord.Guilds.ToFrozenDictionary();
            var dms = discord.PrivateChannels.ToFrozenDictionary();

            var folders = discord.UserSettings?.GuildFolders;
            var ids = new HashSet<ulong>();
            if (folders != null)
            {
                foreach (var folder in folders)
                {
                    if (folder.Id == null || folder.Id == 0)
                    {
                        foreach (var id in folder.GuildIds)
                        {
                            if (!guilds.TryGetValue(id, out var server))
                                continue;

                            Guilds.Add(new GuildListViewModel(server));
                            ids.Add(id);
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

            foreach (var (id, guild) in guilds)
            {
                if (!ids.Contains(id))
                    Guilds.Insert(0, new GuildListViewModel(guild));
            }

            foreach (var (id, dm) in dms.Where(d => d.Value.ReadState?.MentionCount > 0)
                                        .OrderByDescending(d => d.Value.LastMessageId))
            {
                UnreadDMs.Add(new ChannelViewModel(dm.Id, parent: this));
            }
        }

        public bool Navigating { get; internal set; }
        public ObservableCollection<IGuildListViewModel> Guilds { get; }
        public ObservableCollection<ChannelViewModel> UnreadDMs { get; }

        public DiscordUser CurrentUser { get => _currentUser; set => OnPropertySet(ref _currentUser, value); }
        public VoiceConnectionModel VoiceModel { get => _voiceModel; set => OnPropertySet(ref _voiceModel, value); }

        public DiscordChannel CurrentChannel { get => _currentChannel; set => OnPropertySet(ref _currentChannel, value); }
        public ChannelViewModel SelectedDM { get => _selectedDM; set => OnPropertySet(ref _selectedDM, value); }
        public GuildListViewModel SelectedGuild { get => _selectedGuild; set => OnPropertySet(ref _selectedGuild, value); }
        public bool IsFriendsSelected { get => _isFriendsSelected; set => OnPropertySet(ref _isFriendsSelected, value); }
        public bool IsRightPaneOpen { get => _isRightPaneOpen; set => OnPropertySet(ref _isRightPaneOpen, value); }
        public ChannelViewModel PreviousDM { get; set; }

        public string DisplayVersion
        {
            get
            {
                var gitSha = "";
                var versionedAssembly = typeof(DiscordPageViewModel).Assembly;
                var attribute = versionedAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var idx = -1;
                if (attribute != null && (idx = attribute.InformationalVersion.IndexOf('+')) != -1)
                {
                    gitSha = "-" + attribute.InformationalVersion.Substring(idx + 1, 7);
                }

                return $"{Package.Current.Id.Version.ToFormattedString(3)}{gitSha}";
            }
        }

        public GuildListViewModel ViewModelFromGuild(DiscordGuild guild)
        {
            foreach (var guildVM in Guilds)
            {
                if (guildVM.TryGetModelForGuild(guild, out var model))
                    return model;
            }

            return null;
        }

        private Task OnReady(ReadyEventArgs e)
        {
            discord = DiscordManager.Discord;
            syncContext.Post((o) => Load(), null);
            return Task.CompletedTask;
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

        private void UpdateReadState(DiscordDmChannel channel)
        {
            var shouldShow = (channel.ReadState?.MentionCount ?? 0) > 0;
            var dm = UnreadDMs.FirstOrDefault(d => channel.Id == d.Channel.Id);
            syncContext.Post((o) =>
            {
                if (shouldShow)
                {
                    if (dm == null)
                    {
                        dm = new ChannelViewModel(channel.Id, false, this);
                        UnreadDMs.Insert(0, dm);
                    }
                    else
                    {
                        UnreadDMs.Move(UnreadDMs.IndexOf(dm), 0);
                    }
                }
                else
                {
                    UnreadDMs.Remove(dm);
                }
            }, null);
        }

        private Task OnGuildCreated(GuildCreateEventArgs e)
        {
            var vm = this.ViewModelFromGuild(e.Guild);
            if (vm == null)
            {
                syncContext.Post(d => Guilds.Insert(0, new GuildListViewModel(e.Guild)), null);
            }

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            var vm = this.ViewModelFromGuild(e.Guild);
            if (vm == null)
            {
                syncContext.Post(d =>
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
            //var guildPositions = DiscordManager.Discord.UserSettings?.GuildPositions;
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
