using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messaging;

namespace Unicord.Universal.Models
{
    public class FriendsPageViewModel : ViewModelBase
    {
        private SynchronizationContext _syncContext;

        public FriendsPageViewModel()
        {
            All = new ObservableCollection<DiscordRelationship>();
            Online = new ObservableCollection<DiscordRelationship>();
            Blocked = new ObservableCollection<DiscordRelationship>();
            Pending = new ObservableCollection<DiscordRelationship>();

            // capture the current synchronisation context to invoke
            // methods on the dispatcher
            _syncContext = SynchronizationContext.Current;

            foreach (var rel in App.Discord.Relationships.Values.OrderBy(r => r.User?.Username))
            {
                switch (rel.RelationshipType)
                {
                    case DiscordRelationshipType.Friend:
                        All.Add(rel);
                        if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                            Online.Add(rel);
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

            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipAddedEventArgs>(this, (t, v) => t.OnRelationshipAdded(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipRemovedEventArgs>(this, (t, v) => t.OnRelationshipRemoved(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, PresenceUpdateEventArgs>(this, (t, v) => t.OnPresenceUpdated(v.Event));
        }

        public DiscordUser CurrentUser => App.Discord.CurrentUser;
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
                    if (!skipAll)
                    {
                        _syncContext.Post(a =>
                        {
                            var i = All.BinarySearch(rel);
                            if (i < 0)
                                i = ~i;

                            i = Math.Min(i, All.Count - 1);
                            All.Insert(i, rel);
                        }, null);
                    }

                    if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                    {
                        _syncContext.Post(a =>
                        {
                            var i = Online.BinarySearch(rel);
                            if (i < 0)
                                i = ~i;

                            i = Math.Min(i, Online.Count);
                            Online.Insert(i, rel);
                        }, null);
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
                _syncContext.Post(a => All.Remove(rel), null);
            }

            _syncContext.Post(a => Online.Remove(rel), null);
            _syncContext.Post(a => Pending.Remove(rel), null);
            _syncContext.Post(a => Blocked.Remove(rel), null);
        }

        private Task OnPresenceUpdated(PresenceUpdateEventArgs e)
        {
            if (e.User != null && e.PresenceBefore?.Status != e.Status)
            {
                if (App.Discord.Relationships.TryGetValue(e.User.Id, out var rel) &&
                    rel.RelationshipType == DiscordRelationshipType.Friend)
                {
                    SortRelationship(rel, true);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnRelationshipAdded(RelationshipAddedEventArgs e)
        {
            SortRelationship(e.Relationship);
            return Task.CompletedTask;
        }

        private Task OnRelationshipRemoved(RelationshipRemovedEventArgs e)
        {
            RemoveRelationship(e.Relationship);
            return Task.CompletedTask;
        }
    }
}
