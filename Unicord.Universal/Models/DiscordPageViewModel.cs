using System;
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
        private DiscordNavigationService _navigationService;
        private ulong? _selectedGuildId;
        private ulong? _selectedChannelId;

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

        public ObservableCollection<IGuildListViewModel> Guilds { get; }
        public ObservableCollection<ChannelViewModel> UnreadDMs { get; }

        public DiscordUser CurrentUser { get => _currentUser; set => OnPropertySet(ref _currentUser, value); }
        public VoiceConnectionModel VoiceModel { get => _voiceModel; set => OnPropertySet(ref _voiceModel, value); }

        public ChannelViewModel SelectedDM
            => UnreadDMs.FirstOrDefault(d => d.Id == _selectedChannelId);
        public GuildListViewModel SelectedGuild
            => _selectedGuildId != null ? ViewModelFromGuild(_selectedGuildId.Value) : null;
        public bool IsFriendsSelected
            => _selectedGuildId == null;

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

        public void UpdateSelection(ulong? channelId, ulong? guildId)
        {
            if (SelectedGuild != null)
                SelectedGuild.IsSelected = false;

            _selectedChannelId = channelId;
            _selectedGuildId = guildId;

            UnsafeInvokePropertyChanged(nameof(SelectedDM));
            UnsafeInvokePropertyChanged(nameof(SelectedGuild));
            UnsafeInvokePropertyChanged(nameof(IsFriendsSelected));

            if (SelectedGuild != null)
                SelectedGuild.IsSelected = true;
        }

        public GuildListViewModel ViewModelFromGuild(ulong guildId)
        {
            foreach (var guildVM in Guilds)
            {
                if (guildVM.TryGetModelForGuild(guildId, out var model))
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
            var vm = this.ViewModelFromGuild(e.Guild.Id);
            if (vm == null)
            {
                syncContext.Post(d => Guilds.Insert(0, new GuildListViewModel(e.Guild)), null);
            }

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            var vm = this.ViewModelFromGuild(e.Guild.Id);
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
            return Task.CompletedTask;
        }

    }
}
