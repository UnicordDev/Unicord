using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using DSharpPlus.EventArgs;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Models.Relationships;

namespace Unicord.Universal.Models
{
    public class FriendsPageViewModel : ViewModelBase
    {
        public FriendsPageViewModel()
        {
            All = new ObservableCollection<RelationshipViewModel>();
            Online = new ObservableCollection<RelationshipViewModel>();
            Blocked = new ObservableCollection<RelationshipViewModel>();
            Pending = new ObservableCollection<RelationshipViewModel>();

            foreach (var rel in discord.Relationships.Values.OrderBy(r => r.User?.DisplayName))
            {
                var vm = new RelationshipViewModel(rel, this);
                switch (rel.RelationshipType)
                {
                    case DiscordRelationshipType.Friend:
                        All.Add(vm);
                        if (vm.User.Presence != null && vm.User.Presence.Status != UserStatus.Offline)
                            Online.Add(vm);
                        break;
                    case DiscordRelationshipType.Blocked:
                        Blocked.Add(vm);
                        break;
                    case DiscordRelationshipType.IncomingRequest:
                    case DiscordRelationshipType.OutgoingRequest:
                        Pending.Add(vm);
                        break;
                    default:
                        break;
                }
            }

            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipAddEventArgs>(this, (t, v) => t.OnRelationshipAdded(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipRemoveEventArgs>(this, (t, v) => t.OnRelationshipRemoved(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, PresenceUpdateEventArgs>(this, (t, v) => t.OnPresenceUpdated(v.Event));
        }

        public DiscordUser CurrentUser => discord.CurrentUser;
        public ObservableCollection<RelationshipViewModel> All { get; set; }
        public ObservableCollection<RelationshipViewModel> Online { get; set; }
        public ObservableCollection<RelationshipViewModel> Blocked { get; set; }
        public ObservableCollection<RelationshipViewModel> Pending { get; set; }

        private void SortRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            RemoveRelationship(rel, skipAll);

            var viewModel = new RelationshipViewModel(rel, this);
            switch (viewModel.Type)
            {
                case DiscordRelationshipType.Friend:
                    if (!skipAll)
                    {
                        syncContext.Post(a =>
                        {
                            var i = All.BinarySearch(viewModel);
                            if (i < 0)
                                i = ~i;

                            i = Math.Min(i, All.Count - 1);
                            All.Insert(i, viewModel);
                        }, null);
                    }

                    if (viewModel.User.Presence != null && viewModel.User.Presence.Status != UserStatus.Offline)
                    {
                        syncContext.Post(a =>
                        {
                            var i = Online.BinarySearch(viewModel);
                            if (i < 0)
                                i = ~i;

                            i = Math.Min(i, Online.Count);
                            Online.Insert(i, viewModel);
                        }, null);
                    }
                    break;
                case DiscordRelationshipType.Blocked:
                    syncContext.Post(a => Blocked.Add(viewModel), null);
                    break;
                case DiscordRelationshipType.IncomingRequest:
                case DiscordRelationshipType.OutgoingRequest:
                    syncContext.Post(a => Pending.Add(viewModel), null);
                    break;
                default:
                    break;
            }
        }

        private void RemoveRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            if (!skipAll)
            {
                syncContext.Post(a => RemoreRelationship(All, rel), null);
            }

            syncContext.Post(a => RemoreRelationship(Online, rel), null);
            syncContext.Post(a => RemoreRelationship(Pending, rel), null);
            syncContext.Post(a => RemoreRelationship(Blocked, rel), null);
        }

        private void RemoreRelationship(ICollection<RelationshipViewModel> collection, DiscordRelationship rel)
        {
            var old = collection.FirstOrDefault(r => rel.Id == r.Id);
            if (old != null)
                collection.Remove(old);
        }

        private Task OnPresenceUpdated(PresenceUpdateEventArgs e)
        {
            if (e.User != null && e.PresenceBefore?.Status != e.Status)
            {
                if (discord.Relationships.TryGetValue(e.User.Id, out var rel) &&
                    rel.RelationshipType == DiscordRelationshipType.Friend)
                {
                    SortRelationship(rel, true);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnRelationshipAdded(RelationshipAddEventArgs e)
        {
            SortRelationship(e.Relationship);
            return Task.CompletedTask;
        }

        private Task OnRelationshipRemoved(RelationshipRemoveEventArgs e)
        {
            RemoveRelationship(e.Relationship);
            return Task.CompletedTask;
        }
    }
}
