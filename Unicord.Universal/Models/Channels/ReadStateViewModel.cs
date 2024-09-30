using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using CommunityToolkit.Mvvm.Messaging;

namespace Unicord.Universal.Models.Channels
{
    public class ReadStateViewModel : ViewModelBase
    {
        protected readonly ulong channelId;
        protected readonly ChannelViewModel viewModel;

        protected DiscordReadState readState;

        public ReadStateViewModel(ulong channelId, ChannelViewModel channelViewModel = null)
            : base(channelViewModel)
        {
            this.channelId = channelId;
            this.viewModel = channelViewModel ?? new ChannelViewModel(channelId, true, this);

            if (!discord.ReadStates.TryGetValue(channelId, out readState))
                readState = null;

            WeakReferenceMessenger.Default.Register<ReadStateViewModel, ChannelUnreadUpdateEventArgs>(this, (r, m) => r.OnChannelUnreadUpdate(m.Event));
            WeakReferenceMessenger.Default.Register<ReadStateViewModel, ReadStateUpdateEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
        }

        private void OnChannelUnreadUpdate(ChannelUnreadUpdateEventArgs e)
        {
            if (!e.ReadStates.TryGetValue(channelId, out var newReadState))
                return;

            readState = newReadState;
            InvokePropertyChanged(nameof(Unread));
            InvokePropertyChanged(nameof(MentionCount));
            InvokePropertyChanged(nameof(LastMessageId));
        }

        private void OnReadStateUpdated(ReadStateUpdateEventArgs e)
        {
            if (e.ReadState.Id != channelId)
                return;

            readState = e.ReadState;
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
                if (readState == null || readState.Id == 0)
                    return false;

                if (discord == null)
                    return false;

                if (!discord.TryGetCachedChannel(channelId, out var channel)) 
                    return false; // in theory impossible

                if (channel.Type == ChannelType.Voice || channel.Type == ChannelType.Category)
                    return false;

                if (channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
                {
                    return readState.MentionCount > 0;
                }

                return (readState.MentionCount > 0 || (channel.LastMessageId != 0 && channel.LastMessageId > readState.LastMessageId));
            }
        }

        public int MentionCount
            => readState?.MentionCount ?? 0;
        public ulong LastMessageId
            => readState?.LastMessageId ?? 0;
    }
}
