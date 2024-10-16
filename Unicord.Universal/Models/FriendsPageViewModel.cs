using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using DSharpPlus.EventArgs;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Relationships;
using Unicord.Universal.Services;

namespace Unicord.Universal.Models
{
    public class FriendsPageViewModel : ViewModelBase
    {
        public FriendsPageViewModel()
        {
            All = [];
            Online = [];
            Blocked = [];
            Pending = [];

            Load();
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, ReadyEventArgs>(this, (t, v) => t.OnReady(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipAddEventArgs>(this, (t, v) => t.OnRelationshipAdded(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, RelationshipRemoveEventArgs>(this, (t, v) => t.OnRelationshipRemoved(v.Event));
            WeakReferenceMessenger.Default.Register<FriendsPageViewModel, PresenceUpdateEventArgs>(this, (t, v) => t.OnPresenceUpdated(v.Event));
        }

        private void Load()
        {
            if (discord == null)
                return;

            All.Clear();
            Online.Clear();
            Blocked.Clear();
            Pending.Clear();

            foreach (var (id, rel) in discord.Relationships.OrderBy(r => r.Value.User?.DisplayName))
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
        }

        public DiscordUser CurrentUser => discord.CurrentUser;
        public ObservableCollection<RelationshipViewModel> All { get; private set; }
        public ObservableCollection<RelationshipViewModel> Online { get; private set; }
        public ObservableCollection<RelationshipViewModel> Blocked { get; private set; }
        public ObservableCollection<RelationshipViewModel> Pending { get; private set; }

        private void SortRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            RemoveRelationship(rel, skipAll);

            var viewModel = new RelationshipViewModel(rel, this);
            switch (viewModel.Type)
            {
                case DiscordRelationshipType.Friend:
                    if (!skipAll)
                    {
                        var i = All.BinarySearch(viewModel);
                        if (i < 0)
                            i = ~i;

                        i = Math.Min(i, All.Count - 1);
                        All.Insert(i, viewModel);
                    }

                    if (viewModel.User.Presence != null && viewModel.User.Presence.Status != UserStatus.Offline)
                    {
                        var i = Online.BinarySearch(viewModel);
                        if (i < 0)
                            i = ~i;

                        i = Math.Min(i, Online.Count);
                        Online.Insert(i, viewModel);
                    }
                    break;
                case DiscordRelationshipType.Blocked:
                    Blocked.Add(viewModel);
                    break;
                case DiscordRelationshipType.IncomingRequest:
                case DiscordRelationshipType.OutgoingRequest:
                    Pending.Add(viewModel);
                    break;
                default:
                    break;
            }
        }

        private void RemoveRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            RemoveRelationship(All, rel);
            RemoveRelationship(Online, rel);
            RemoveRelationship(Pending, rel);
            RemoveRelationship(Blocked, rel);
        }

        private void RemoveRelationship(ICollection<RelationshipViewModel> collection, DiscordRelationship rel)
        {
            var old = collection.FirstOrDefault(r => rel.Id == r.Id);
            if (old != null)
                collection.Remove(old);
        }

        private Task OnReady(ReadyEventArgs e)
        {
            syncContext.Post((o) =>
            {
                discord = DiscordManager.Discord;
                Load();
            }, null);
            return Task.CompletedTask;
        }

        private Task OnPresenceUpdated(PresenceUpdateEventArgs e)
        {
            if (e.User != null && e.PresenceBefore?.Status != e.Status)
            {
                if (discord.Relationships.TryGetValue(e.User.Id, out var rel) &&
                    rel.RelationshipType == DiscordRelationshipType.Friend)
                {
                    syncContext.Post(_ => SortRelationship(rel, true), null);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnRelationshipAdded(RelationshipAddEventArgs e)
        {
            syncContext.Post(_ => SortRelationship(e.Relationship), null);
            return Task.CompletedTask;
        }

        private Task OnRelationshipRemoved(RelationshipRemoveEventArgs e)
        {
            syncContext.Post(_ => RemoveRelationship(e.Relationship), null);
            return Task.CompletedTask;
        }
    }
}
