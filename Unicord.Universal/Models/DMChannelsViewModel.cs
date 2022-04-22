using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Unicord.Universal.Models
{
    public class DMChannelsViewModel : ViewModelBase, IDisposable
    {
        private SynchronizationContext _syncContext;
        private int _selectedItem = -1;

        public DMChannelsViewModel()
        {
            _syncContext = SynchronizationContext.Current;
            DMChannels = new ObservableCollection<DiscordDmChannel>(App.Discord.PrivateChannels.Values.OrderByDescending(r => r.ReadState?.LastMessageId));
            App.Discord.DmChannelCreated += OnDmCreated;
            App.Discord.DmChannelDeleted += OnDmDeleted;
            App.Discord.MessageCreated += OnMessageCreated;
        }

        public ObservableCollection<DiscordDmChannel> DMChannels { get; set; }

        public int SelectedIndex { get => _selectedItem; set => OnPropertySet(ref _selectedItem, value); }
        public bool UpdatingIndex { get; set; }

        public void Dispose()
        {
            App.Discord.DmChannelCreated -= OnDmCreated;
            App.Discord.DmChannelDeleted -= OnDmDeleted;
            App.Discord.MessageCreated -= OnMessageCreated;
        }

        private Task OnDmCreated(DmChannelCreateEventArgs e)
        {
            _syncContext.Post(o => DMChannels.Insert(0, o as DiscordDmChannel), e.Channel);
            return Task.CompletedTask;
        }

        private Task OnDmDeleted(DmChannelDeleteEventArgs e)
        {
            _syncContext.Post(o => DMChannels.Remove(o as DiscordDmChannel), e.Channel);
            return Task.CompletedTask;
        }

        private Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                var current = DMChannels.ElementAtOrDefault(SelectedIndex);
                var index = DMChannels.IndexOf(dm);
                _syncContext.Post(o =>
                {
                    // BUGBUG: this is very bad
                    UpdatingIndex = true;
                    if (index > 0)
                    {
                        DMChannels.Move(index, 0);
                    }
                    else if (index < 0)
                    {
                        DMChannels.Insert(0, dm);
                    }

                    SelectedIndex = current == null ? -1 : DMChannels.IndexOf(current);
                    UpdatingIndex = false;
                }, null);
            }

            return Task.CompletedTask;
        }
    }
}
