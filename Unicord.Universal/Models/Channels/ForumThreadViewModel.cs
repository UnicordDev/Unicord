using System;
using System.Linq;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Channels
{
    public class ForumThreadViewModel : ChannelPageViewModelBase
    {
        private MessageViewModel firstMessage;
        private UserViewModel creator;

        internal ForumThreadViewModel(DiscordThreadChannel channel, DiscordMessage firstMessage, ViewModelBase parent = null)
            : base(channel, false, parent)
        {
            if (Channel is not DiscordThreadChannel thread)
                throw new InvalidOperationException();

            if (thread.FirstMessage != null)
                this.firstMessage = new MessageViewModel(thread.FirstMessage);
            else if (firstMessage != null)
                this.firstMessage = new MessageViewModel(firstMessage);


            creator = new UserViewModel(thread.CreatorId, thread.GuildId, this);
        }

        public DiscordThreadChannel Thread
            => (DiscordThreadChannel)Channel;

        public MessageViewModel FirstMessage
        {
            get => firstMessage;
            internal set
            {
                OnPropertySet(ref firstMessage, value);
                InvokePropertyChanged(nameof(ShowAttachment));
                InvokePropertyChanged(nameof(DisplayAttachment));
            }
        }

        public UserViewModel Creator
        {
            get => creator;
            internal set
            {
                OnPropertySet(ref creator, value);
                InvokePropertyChanged(nameof(HasValidCreator));
            }
        }

        public bool HasValidCreator
            => !string.IsNullOrWhiteSpace(Creator.DisplayName);

        public bool ShowAttachment
            => DisplayAttachment != null;

        public AttachmentViewModel DisplayAttachment
            => FirstMessage?.Attachments.FirstOrDefault(a => !double.IsNaN(a.NaturalWidth));
    }
}
