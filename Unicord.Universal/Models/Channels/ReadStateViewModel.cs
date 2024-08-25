using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Models;

namespace Unicord.Universal.Models.Channels
{
    public class ReadStateViewModel : ViewModelBase
    {
        protected readonly DiscordChannel channel;
        protected readonly DiscordReadState readState;
        protected readonly ChannelViewModel viewModel;

        public ReadStateViewModel(DiscordChannel channel, ChannelViewModel channelViewModel = null)
            : base(channelViewModel)
        {
            this.channel = channel;
            this.viewModel = channelViewModel ?? new ChannelViewModel(channel);

            if (!App.Discord.ReadStates.TryGetValue(channel.Id, out readState))
                readState = App.Discord.DefaultReadState;

            WeakReferenceMessenger.Default.Register<ReadStateViewModel, ReadStateUpdatedEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
        }

        private void OnReadStateUpdated(ReadStateUpdatedEventArgs e)
        {
            if (e.ReadState.Id != readState.Id)
                return;

            InvokePropertyChanged(nameof(Unread));
            InvokePropertyChanged(nameof(MentionCount));
            InvokePropertyChanged(nameof(LastMessageId));
        }

        /// <summary>
        /// Is the channel unread according to the read state. <para/>Notably does not take channel mutes into account.
        /// </summary>
        public bool Unread
        {
            get
            {
                // this shit should never happen but apparently it does sometimes, don't question it
                if (readState.Id == 0)
                    return false;

                if (discord == null || discord.IsDisposed)
                    return false;

                if (channel.Type == ChannelType.Voice || channel.Type == ChannelType.Category || channel.Type == ChannelType.Store)
                    return false;

                if (channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
                {
                    return readState.MentionCount > 0;
                }

                return (readState.MentionCount > 0 || (channel.LastMessageId != 0 && channel.LastMessageId > readState.LastMessageId));
            }
        }

        public int MentionCount
            => readState.MentionCount;
        public ulong LastMessageId
            => readState.LastMessageId;
    }
}
