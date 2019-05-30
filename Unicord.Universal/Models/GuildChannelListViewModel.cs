using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WamWooWam.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Models
{
    public class GuildChannelListViewModel : PropertyChangedBase, IDisposable
    {
        private DiscordGuild _guild;
        private SynchronizationContext _syncContext;
        private bool _canEdit;

        public GuildChannelListViewModel(DiscordGuild guild)
        {
            _guild = guild;
            _syncContext = SynchronizationContext.Current;
            ViewSource = new CollectionViewSource();

            InitialiseLists();

            App.Discord.ChannelCreated += Discord_ChannelCreated;
            App.Discord.ChannelUpdated += Discord_ChannelUpdated;
            App.Discord.ChannelDeleted += Discord_ChannelDeleted;
        }

        public CollectionViewSource ViewSource { get; set; }
        public ObservableCollection<GuildChannelGroup> ChannelGroups { get; set; }
        public ObservableCollection<DiscordChannel> Channels { get; set; }
        public ListViewReorderMode ReorderMode => CanEdit ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;

        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                OnPropertySet(ref _canEdit, value);
                InvokePropertyChanged(nameof(ReorderMode));
            }
        }

        private Task Discord_ChannelCreated(ChannelCreateEventArgs e)
        {
            if (e.Guild.Id == _guild.Id)
            {
                var group = GetOrCreateGroup(e.Channel);
                if (e.Channel.Type != ChannelType.Category)
                {
                    var i = (e.Channel.Parent?.Children ?? e.Guild.Channels.Values)
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Position)
                    .ToList()
                    .IndexOf(e.Channel);

                    _syncContext.Post(o =>
                    {
                        // remove the channel and re-insert this
                        group.Insert(i, e.Channel);
                    }, null);
                }
            }

            return Task.CompletedTask;
        }

        private Task Discord_ChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.Guild.Id == _guild.Id && e.ChannelAfter.Type != ChannelType.Group)
            {
                var canAccess = e.ChannelAfter.PermissionsFor(_guild.CurrentMember).HasPermission(Permissions.AccessChannels);
                if (!canAccess)
                {
                    // if the channel is inaccessable, remove it and return
                    RemoveFromAll(e.ChannelAfter);
                    return Task.CompletedTask;
                }

                if (e.ChannelAfter.ParentId != null && ChannelGroups == null)
                {
                    // if we now have a group, but the list isn't grouped, re create the 
                    // lists to account for this
                    _syncContext.Post(o => InitialiseLists(), null);
                    return Task.CompletedTask;
                }

                if (e.ChannelBefore.ParentId != e.ChannelAfter.ParentId)
                {
                    // if the channels's group has changed, remove it from it's old group
                    RemoveFromGroup(e.ChannelAfter, e.ChannelBefore.ParentId);

                    // then find it's new group
                    var group = GetOrCreateGroup(e.ChannelAfter);
                    if (!group.Contains(e.ChannelAfter))
                    {
                        // find the position in the new group, and insert it there
                        var i = (e.ChannelAfter.Parent?.Children ?? e.Guild.Channels.Values)
                            .OrderBy(c => c.Type)
                            .ThenBy(c => c.Position)
                            .ToList()
                            .IndexOf(e.ChannelAfter);

                        _syncContext.Post(o => group.Insert(i, e.ChannelAfter), null);
                    }
                }
                else if (e.ChannelBefore.Position != e.ChannelAfter.Position)
                {
                    // if just the channel's position has changed, find it's group and index
                    var group = GetOrCreateGroup(e.ChannelAfter);
                    if (e.ChannelAfter.Type == ChannelType.Category)
                    {
                        // if it's a category, move the category    
                        
                        var i = e.Guild.Channels.Values
                            .Where(c => c.Type == ChannelType.Category)
                            .OrderBy(c => c.Position)
                            .ToList()
                            .IndexOf(e.ChannelAfter);

                        _syncContext.Post(o =>
                        {
                            // remove the channel and re-insert this group
                            ChannelGroups.Remove(group as GuildChannelGroup);
                            ChannelGroups.Insert(i, group as GuildChannelGroup);
                        }, null);
                    }
                    else
                    {
                        var i = (e.ChannelAfter.Parent?.Children ?? e.Guild.Channels.Values)
                            .OrderBy(c => c.Type)
                            .ThenBy(c => c.Position)
                            .ToList()
                            .IndexOf(e.ChannelAfter);

                        _syncContext.Post(o =>
                        {
                            // remove the channel and re-insert this
                            group.Remove(e.ChannelAfter);
                            group.Insert(i, e.ChannelAfter);
                        }, null);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task Discord_ChannelDeleted(ChannelDeleteEventArgs e)
        {
            if (e.Guild.Id == _guild.Id)
            {
                RemoveFromAll(e.Channel);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a channel's group, or creates one if it doesn't already exist.
        /// </summary>
        private IList<DiscordChannel> GetOrCreateGroup(DiscordChannel channel)
        {
            if (ChannelGroups == null)
            {
                return Channels;
            }
            else
            {
                if (channel.Type == ChannelType.Category)
                {
                    var group = ChannelGroups.FirstOrDefault(g => g.Key?.Id == channel.Id);
                    if (group == null)
                    {
                        group = new GuildChannelGroup(channel, Enumerable.Empty<DiscordChannel>());
                        var i = ChannelGroups.BinarySearch(channel).Clamp(0, ChannelGroups.Count - 1);
                        _syncContext.Post(o => ChannelGroups.Insert(i, group), null);
                    }

                    return group;
                }
                else
                {
                    var group = ChannelGroups.FirstOrDefault(g => g.Key?.Id == channel.ParentId);
                    if (group == null)
                    {
                        group = new GuildChannelGroup(channel.Parent, Enumerable.Empty<DiscordChannel>());
                        var i = ChannelGroups.BinarySearch(channel.Parent).Clamp(0, ChannelGroups.Count - 1);
                        _syncContext.Post(o => ChannelGroups.Insert(i, group), null);
                    }

                    return group;
                }
            }
        }

        /// <summary>
        /// Removes a channel from a specificed group.
        /// </summary>
        private void RemoveFromGroup(DiscordChannel channel, ulong? parentId)
        {
            if (ChannelGroups == null)
            {
                Channels.Remove(channel);
            }
            else
            {
                var group = ChannelGroups.FirstOrDefault(g => g.Key?.Id == parentId);
                if (group != null)
                {
                    _syncContext.Post(o => group.Remove(channel), null);
                }
            }
        }

        /// <summary>
        /// Removes a channel from all groups
        /// </summary>
        private void RemoveFromAll(DiscordChannel channel)
        {
            if (ChannelGroups != null)
            {
                foreach (var group in ChannelGroups)
                {
                    if (group.Key?.Id == channel.Id)
                    {
                        _syncContext.Post(o => ChannelGroups.Remove(group), null);
                        break;
                    }

                    if (group.Contains(channel))
                    {
                        _syncContext.Post(o => group.Remove(channel), null);
                    }
                }
            }
            else
            {
                _syncContext.Post(o => Channels.Remove(channel), null);
            }
        }

        /// <summary>
        /// Initially populates the server list
        /// </summary>
        private void InitialiseLists()
        {
            ChannelGroups = null;
            Channels = null;

            var currentMember = _guild.CurrentMember;
            var permissions = currentMember.PermissionsIn(_guild.Channels.Values.FirstOrDefault());
            CanEdit = permissions.HasPermission(Permissions.ManageChannels);

            var channels = _guild.IsOwner ?
                _guild.Channels.Values :
                _guild.Channels.Values.Where(c => c.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels));

            if (channels.Any(c => c.IsCategory))
            {
                // Use new discord channel category behaviour
                var groupedChannels = channels
                    .Where(c => !c.IsCategory)
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Position)
                    .GroupBy(g => g.Parent)
                    .OrderBy(c => c.Key?.Position)
                    .Select(g => new GuildChannelGroup(g.Key, g));

                ChannelGroups = new ObservableCollection<GuildChannelGroup>(groupedChannels);
                ViewSource.IsSourceGrouped = true;
                ViewSource.Source = ChannelGroups;
            }
            else
            {
                // Use old discord non-category behaviour
                var orderedChannels = channels
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Position);

                Channels = new ObservableCollection<DiscordChannel>(orderedChannels);
                ViewSource.IsSourceGrouped = false;
                ViewSource.Source = Channels;
            }
        }

        public void Dispose()
        {
            App.Discord.ChannelCreated -= Discord_ChannelCreated;
            App.Discord.ChannelUpdated -= Discord_ChannelUpdated;
            App.Discord.ChannelDeleted -= Discord_ChannelDeleted;
        }
    }

    public class GuildChannelGroup : 
        IGrouping<DiscordChannel, DiscordChannel>, 
        ICollection<DiscordChannel>,
        IList<DiscordChannel>, 
        IComparable<DiscordChannel>,
        IList, INotifyCollectionChanged
    {
        private ObservableCollection<DiscordChannel> _channels;

        public GuildChannelGroup(DiscordChannel key, IEnumerable<DiscordChannel> channels)
        {
            Key = key;
            _channels = new ObservableCollection<DiscordChannel>(channels);
        }

        public DiscordChannel this[int index] { get => _channels[index]; set => _channels[index] = value; }

        public DiscordChannel Key { get; }

        public int Count => _channels.Count;
        public bool IsReadOnly => ((ICollection<DiscordChannel>)_channels).IsReadOnly;

        public bool IsFixedSize => ((IList)_channels).IsFixedSize;
        public bool IsSynchronized => ((IList)_channels).IsSynchronized;
        public object SyncRoot => ((IList)_channels).SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _channels.CollectionChanged += value;
            remove => _channels.CollectionChanged -= value;
        }

        public void Add(DiscordChannel item) => _channels.Add(item);
        public void Clear() => _channels.Clear();
        public bool Contains(DiscordChannel item) => _channels.Contains(item);
        public void CopyTo(DiscordChannel[] array, int arrayIndex) => _channels.CopyTo(array, arrayIndex);
        public IEnumerator<DiscordChannel> GetEnumerator() => _channels.GetEnumerator();
        public int IndexOf(DiscordChannel item) => _channels.IndexOf(item);
        public void Insert(int index, DiscordChannel item) => _channels.Insert(index.Clamp(0, Count), item);
        public bool Remove(DiscordChannel item) => _channels.Remove(item);
        public void RemoveAt(int index) => _channels.RemoveAt(index);

        public void Move(int oldIndex, int newIndex) => _channels.Move(oldIndex, newIndex);

        object IList.this[int index] { get => ((IList)_channels)[index]; set => ((IList)_channels)[index] = value; }
        int IList.Add(object value) => ((IList)_channels).Add(value);
        bool IList.Contains(object value) => ((IList)_channels).Contains(value);
        int IList.IndexOf(object value) => ((IList)_channels).IndexOf(value);
        void IList.Insert(int index, object value) => ((IList)_channels).Insert(index, value);
        void IList.Remove(object value) => ((IList)_channels).Remove(value);

        void ICollection.CopyTo(Array array, int index) => ((IList)_channels).CopyTo(array, index);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_channels).GetEnumerator();
        public int CompareTo(DiscordChannel other) => ((IComparable<DiscordChannel>)Key).CompareTo(other);
    }
}
