using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;

namespace Unicord.Universal.Models
{
    public class DMChannelsViewModel : ViewModelBase
    {
        private int _selectedItem = -1;

        public DMChannelsViewModel() : base(null)
        {
            DMChannels = new ObservableCollection<DmChannelListViewModel>(
                App.Discord.PrivateChannels.Values
                .Select(s => new DmChannelListViewModel(s))
                .OrderByDescending(r => r.ReadState?.LastMessageId));

            WeakReferenceMessenger.Default.Register<DMChannelsViewModel, DmChannelCreateEventArgs>(this, (r, e) => r.OnDmCreated(e.Event));
            WeakReferenceMessenger.Default.Register<DMChannelsViewModel, DmChannelDeleteEventArgs>(this, (r, e) => r.OnDmDeleted(e.Event));
            WeakReferenceMessenger.Default.Register<DMChannelsViewModel, MessageCreateEventArgs>(this, (r, e) => r.OnMessageCreated(e.Event));
        }

        public ObservableCollection<DmChannelListViewModel> DMChannels { get; set; }

        public int SelectedIndex { get => _selectedItem; set => OnPropertySet(ref _selectedItem, value); }
        public bool UpdatingIndex { get; set; }

        private Task OnDmCreated(DmChannelCreateEventArgs e)
        {
            syncContext.Post(o => DMChannels.Insert(0, new DmChannelListViewModel(o as DiscordDmChannel)), e.Channel);
            return Task.CompletedTask;
        }

        private Task OnDmDeleted(DmChannelDeleteEventArgs e)
        {
            syncContext.Post(o => DMChannels.Remove(DMChannels.FirstOrDefault(s => s.Channel == o as DiscordDmChannel)), e.Channel);
            return Task.CompletedTask;
        }

        private Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                var current = DMChannels.ElementAtOrDefault(SelectedIndex);
                var index = DMChannels.FindIndex(s => s.Channel == dm);
                if (index == 0)
                    return Task.CompletedTask;

                syncContext.Post(o =>
                {
                    // BUGBUG: this is very bad
                    UpdatingIndex = true;
                    if (index > 0)
                    {
                        DMChannels.Move(index, 0);
                    }
                    else if (index < 0)
                    {
                        DMChannels.Insert(0, new DmChannelListViewModel(dm));
                    }

                    SelectedIndex = current == null ? -1 : DMChannels.IndexOf(current);
                    UpdatingIndex = false;
                }, null);
            }

            return Task.CompletedTask;
        }
    }
}
