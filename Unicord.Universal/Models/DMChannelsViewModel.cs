using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.UI;

namespace Unicord.Universal.Models
{
    public class DMChannelsViewModel : PropertyChangedBase, IDisposable
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
                var index = DMChannels.IndexOf(dm);
                var wasSelected = index == _selectedItem;
                var newIndex = wasSelected ? 0 : SelectedIndex != -1 ? SelectedIndex + 1 : -1;

                if (index > 0)
                {
                    _syncContext.Post(o => DMChannels.Move(index, 0), null);
                }
                else if (index < 0)
                {
                    _syncContext.Post(o => DMChannels.Insert(0, dm), null);
                }

                _syncContext.Post(o => SelectedIndex = newIndex, null);
            }

            return Task.CompletedTask;
        }
    }
}
