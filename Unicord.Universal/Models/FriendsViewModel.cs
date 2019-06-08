using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Unicord.Universal.Models
{
    public class FriendsViewModel : PropertyChangedBase, IDisposable
    {
        private SynchronizationContext _syncContext;

        // for binary searches
        private List<DiscordRelationship> _allList;
        private List<DiscordRelationship> _onlineList;

        public FriendsViewModel()
        {
            All = new ObservableCollection<DiscordRelationship>();
            Online = new ObservableCollection<DiscordRelationship>();
            Blocked = new ObservableCollection<DiscordRelationship>();
            Pending = new ObservableCollection<DiscordRelationship>();

            _allList = new List<DiscordRelationship>();
            _onlineList = new List<DiscordRelationship>();

            // capture the current synchronisation context to invoke
            // methods on the dispatcher
            _syncContext = SynchronizationContext.Current;

            foreach (var rel in App.Discord.Relationships.Values.OrderBy(r => r.User?.Username))
            {
                switch (rel.RelationshipType)
                {
                    case DiscordRelationshipType.Friend:
                        All.Add(rel);
                        _allList.Add(rel);
                        if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                        {
                            Online.Add(rel);
                            _onlineList.Add(rel);
                        }
                        break;
                    case DiscordRelationshipType.Blocked:
                        Blocked.Add(rel);
                        break;
                    case DiscordRelationshipType.IncomingRequest:
                    case DiscordRelationshipType.OutgoingRequest:
                        Pending.Add(rel);
                        break;
                    default:
                        break;
                }
            }

            App.Discord.RelationshipAdded += Discord_RelationshipAdded;
            App.Discord.RelationshipRemoved += Discord_RelationshipRemoved;
            App.Discord.PresenceUpdated += Discord_PresenceUpdated;
        }

        public ObservableCollection<DiscordRelationship> All { get; set; }

        public ObservableCollection<DiscordRelationship> Online { get; set; }

        public ObservableCollection<DiscordRelationship> Blocked { get; set; }

        public ObservableCollection<DiscordRelationship> Pending { get; set; }

        private void SortRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            RemoveRelationship(rel, skipAll);

            switch (rel.RelationshipType)
            {
                case DiscordRelationshipType.Friend:
                    int i;

                    if (!skipAll)
                    {
                        i = _allList.BinarySearch(rel);
                        if (i < 0)
                        {
                            i = ~i;
                        }

                        _allList.Insert(i, rel);
                        _syncContext.Post(a => All.Insert(i, rel), null);
                    }

                    if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                    {
                        i = _onlineList.BinarySearch(rel);
                        if (i < 0)
                        {
                            i = ~i;
                        }

                        _onlineList.Insert(i, rel);
                        _syncContext.Post(a => Online.Insert(i, rel), null);
                    }
                    break;
                case DiscordRelationshipType.Blocked:
                    _syncContext.Post(a => Blocked.Add(rel), null);
                    break;
                case DiscordRelationshipType.IncomingRequest:
                case DiscordRelationshipType.OutgoingRequest:
                    _syncContext.Post(a => Pending.Add(rel), null);
                    break;
                default:
                    break;
            }
        }

        private void RemoveRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            if (!skipAll)
            {
                if (_allList.Remove(rel))
                    _syncContext.Post(a => All.Remove(rel), null);
            }

            if (_onlineList.Remove(rel))
                _syncContext.Post(a => Online.Remove(rel), null);

            if (Pending.Contains(rel)) // is this actually faster?
                _syncContext.Post(a => Pending.Remove(rel), null);

            if (Blocked.Contains(rel)) // ^^
                _syncContext.Post(a => Blocked.Remove(rel), null);
        }

        private Task Discord_PresenceUpdated(PresenceUpdateEventArgs e)
        {
            if (e.User != null && e.PresenceBefore?.Status != e.Status)
            {
                if (App.Discord.Relationships.TryGetValue(e.User.Id, out var rel) && rel?.RelationshipType == DiscordRelationshipType.Friend)
                {
                    SortRelationship(rel, true);
                }
            }

            return Task.CompletedTask;
        }

        private Task Discord_RelationshipAdded(RelationshipEventArgs e)
        {
            SortRelationship(e.Relationship);
            return Task.CompletedTask;
        }

        private Task Discord_RelationshipRemoved(RelationshipEventArgs e)
        {
            RemoveRelationship(e.Relationship);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (App.Discord != null)
            {
                App.Discord.RelationshipAdded -= Discord_RelationshipAdded;
                App.Discord.RelationshipRemoved -= Discord_RelationshipRemoved;
                App.Discord.PresenceUpdated -= Discord_PresenceUpdated;
            }
        }
    }
}
