using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Commands;
using Unicord.Universal.Commands.Generic;
using Unicord.Universal.Commands.Messages;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Messages.Components;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Models.User;
using Windows.ApplicationModel.Resources;

namespace Unicord.Universal.Models.Messages
{
    public partial class MessageViewModel : ViewModelBase, ISnowflake
    {
        private static readonly ResourceLoader _strings
            = ResourceLoader.GetForViewIndependentUse("Converters");

        private ChannelViewModel _channelViewModelCache;
        private UserViewModel _userViewModelCache;

        public MessageViewModel(DiscordMessage discordMessage, ChannelPageViewModel parent = null, MessageViewModel parentMessage = null)
            : base((ViewModelBase)parentMessage ?? parent)
        {
            Message = discordMessage;
            Parent = parent;

            _channelViewModelCache = parent;

            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageUpdateEventArgs>(this, (t, e) => t.OnMessageUpdated(e.Event));

            // we dont wanna do this for replies
            if (parentMessage == null)
            {
                ReplyCommand = new ReplyCommand(this);
                CopyMessageCommand = new CopyMessageCommand(this);
                CopyUrlCommand = new CopyUrlCommand(this);
                CopyIdCommand = new CopyIdCommand(this);
                PinCommand = new PinMessageCommand(this);
                DeleteCommand = new DeleteMessageCommand(this);
                ReactCommand = new ReactCommand(this);

                var embedModels = GetGroupedEmbeds(Message);
                Embeds = new ObservableCollection<EmbedViewModel>(embedModels);
                Attachments = new ObservableCollection<AttachmentViewModel>(Message.Attachments.Select(a => new AttachmentViewModel(a, this)));
                Stickers = new ObservableCollection<StickerViewModel>(Message.Stickers.Select(s => new StickerViewModel(s, this)));
                Components = new ObservableCollection<ComponentViewModelBase>(Message.Components.Select(ComponentViewModelFactory));
                Reactions = new ObservableCollection<ReactionViewModel>(Message.Reactions.Select(r => new ReactionViewModel(r, ReactCommand)));

                WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionAddEventArgs>(this,
                    (t, e) => t.OnReactionAdded(e.Event));
                WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionRemoveEventArgs>(this,
                    (t, e) => t.OnReactionRemoved(e.Event));
                WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionRemoveEmojiEventArgs>(this,
                    (t, e) => t.OnReactionGroupRemoved(e.Event));
                WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionsClearEventArgs>(this,
                    (t, e) => t.OnReactionsCleared(e.Event));
            }
        }

        private List<EmbedViewModel> GetGroupedEmbeds(DiscordMessage message)
        {
            var embedGroups = message.Embeds
                .GroupBy(g => g.Url);

            var embedModels = new List<EmbedViewModel>();
            foreach (var group in embedGroups)
            {
                if (group.Key == null)
                    embedModels.AddRange(group.Select(s => new EmbedViewModel(s, [], this)));
                else
                    embedModels.Add(new EmbedViewModel(group.First(), group.Skip(1).ToArray(), this));
            }

            return embedModels;
        }

        public DiscordMessage Message { get; }

        public ChannelPageViewModel Parent { get; }

        public ulong Id
            => Message.Id;
        public MessageViewModel ReferencedMessage
            => Message.ReferencedMessage != null ?
                new MessageViewModel(Message.ReferencedMessage, Parent, this) :
                null;
        public ChannelViewModel Channel
            => _channelViewModelCache ??=
                (Message.Channel != null ? new ChannelViewModel(Message.Channel, true, this) : new ChannelViewModel(Message.ChannelId, true, this));

        public UserViewModel Author
            => _userViewModelCache ??= new UserViewModel(Message.Author, Channel.Channel.GuildId, this);
        public DateTimeOffset Timestamp
            => Message.Timestamp;
        public string Content
            => Message.Content;
        public bool IsEdited
            => Message.IsEdited;
        public bool IsSystemMessage
            => Message.MessageType is not (MessageType.Default or MessageType.Reply or
                MessageType.ApplicationCommand or MessageType.ContextMenuCommand);
        public MessageType Type
            => Message.MessageType ?? MessageType.Default;

        public string SystemMessageText => Message.MessageType switch
        {
            MessageType.RecipientAdd => string.Format(_strings.GetString("UserJoinedGroupFormat"), Author.Mention),
            MessageType.RecipientRemove => string.Format(_strings.GetString("UserLeftGroupFormat"), Author.Mention),
            MessageType.Call => string.Format(_strings.GetString("UserStartedCallFormat"), Author.Mention),
            MessageType.ChannelNameChange => string.Format(_strings.GetString("UserChannelNameChangeFormat"), Author.Mention),
            MessageType.ChannelIconChange => string.Format(_strings.GetString("UserChannelIconChangeFormat"), Author.Mention),
            MessageType.ChannelPinnedMessage => string.Format(_strings.GetString("UserMessagePinFormat"), Author.Mention),
            MessageType.GuildMemberJoin => string.Format(WelcomeStrings[(int)(Message.CreationTimestamp.ToUnixTimeMilliseconds() % WelcomeStrings.Length)], Author.Mention),
            MessageType.UserPremiumGuildSubscription => string.Format(_strings.GetString(
                                        string.IsNullOrWhiteSpace(Content) ?
                                        "UserPremiumGuildSubscriptionFormat" :
                                        "UserPremiumMultiGuildSubscriptionFormat"), Author.Mention, Content),
            MessageType.TierOneUserPremiumGuildSubscription => string.Format(_strings.GetString(
                                        string.IsNullOrWhiteSpace(Content) ?
                                        "UserPremiumGuildSubscriptionTierFormat" :
                                        "UserPremiumMultiGuildSubscriptionTierFormat"), Author.Mention, Content, 1),
            MessageType.TierTwoUserPremiumGuildSubscription => string.Format(_strings.GetString(
                                        string.IsNullOrWhiteSpace(Content) ?
                                        "UserPremiumGuildSubscriptionTierFormat" :
                                        "UserPremiumMultiGuildSubscriptionTierFormat"), Author.Mention, Content, 2),
            MessageType.TierThreeUserPremiumGuildSubscription => string.Format(_strings.GetString(
                                        string.IsNullOrWhiteSpace(Content) ?
                                        "UserPremiumGuildSubscriptionTierFormat" :
                                        "UserPremiumMultiGuildSubscriptionTierFormat"), Author.Mention, Content, 3),
            _ => $"Unknown system message type {Message.MessageType}! [File an issue!](https://github.com/UnicordDev/Unicord/issues/new)",
        };

        public string SystemMessageSymbol => Message.MessageType switch
        {
            MessageType.RecipientAdd => "\xE72A",
            MessageType.RecipientRemove => "\xE72B",
            MessageType.Call => "\xE717",
            MessageType.ChannelNameChange
                or MessageType.ChannelIconChange => "\xE70F",
            MessageType.ChannelPinnedMessage => "\xE840",
            MessageType.GuildMemberJoin => "\xE72A",
            MessageType.UserPremiumGuildSubscription
                or MessageType.TierOneUserPremiumGuildSubscription
                or MessageType.TierTwoUserPremiumGuildSubscription
                or MessageType.TierThreeUserPremiumGuildSubscription => "\xECAD",
            _ => "\xE783",
        };

        public bool IsMention
        {
            get
            {
                var currentMember = Message.Channel.Guild?.CurrentMember;
                return Message.MentionEveryone || Message.MentionedUsers.Any(u => u?.Id == App.Discord.CurrentUser.Id) ||
                    (currentMember != null && Message.MentionedRoleIds.Any(r => currentMember.RoleIds.Contains(r)));
            }
        }

        // todo: cache probably
        public bool IsCollapsed
        {
            get
            {
                if (Parent == null) return false;

                var index = Parent.Messages.IndexOf(this);
                if (index > 0)
                {
                    if (Parent.Messages[index - 1] is MessageViewModel other
                        && (other.Message.MessageType == MessageType.Default || other.Message.MessageType == MessageType.Reply)
                        && Message.ReferencedMessage == null)
                    {
                        var timeSpan = (Message.CreationTimestamp - other.Message.CreationTimestamp);
                        if (other.Author.User.Id == Message.Author.Id && timeSpan <= TimeSpan.FromMinutes(10))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public ObservableCollection<EmbedViewModel> Embeds { get; }
        public ObservableCollection<AttachmentViewModel> Attachments { get; }
        public ObservableCollection<StickerViewModel> Stickers { get; }
        public ObservableCollection<ComponentViewModelBase> Components { get; }
        public ObservableCollection<ReactionViewModel> Reactions { get; }

        public ICommand ReplyCommand { get; }
        public ICommand CopyMessageCommand { get; }
        public ICommand CopyUrlCommand { get; }
        public ICommand CopyIdCommand { get; }
        public ICommand PinCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ReactCommand { get; }

        private Task OnReactionAdded(MessageReactionAddEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private Task OnReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private Task OnReactionsCleared(MessageReactionsClearEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            syncContext.Post((o) => ((MessageViewModel)o).Reactions.Clear(), this);
            return Task.CompletedTask;
        }

        private Task OnReactionGroupRemoved(MessageReactionRemoveEmojiEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private void OnMessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return;

            if (e.MessageBefore?.Content != e.Message.Content)
                InvokePropertyChanged(nameof(Content));

            if (e.MessageBefore?.IsEdited != e.Message.IsEdited)
                InvokePropertyChanged(nameof(IsEdited));

            if (e.Message.Attachments.Count != Attachments.Count)
                syncContext.Post(o =>
                {
                    Attachments.Clear();
                    foreach (var attachment in e.Message.Attachments)
                    {
                        Attachments.Add(new AttachmentViewModel(attachment, this));
                    }
                }, null);

            var embedGroups = Message.Embeds
                    .GroupBy(g => g.Url);

            syncContext.Post((o) =>
            {
                Embeds.Clear();
                foreach (var model in GetGroupedEmbeds(e.Message))
                {
                    Embeds.Add(model);
                }
            }, null);

            return;
        }

        private ComponentViewModelBase ComponentViewModelFactory(DiscordComponent component)
        {
            if (component is DiscordActionRowComponent actionRow)
                return new ActionRowComponentViewModel(actionRow, ComponentViewModelFactory, this);

            if (component is DiscordButtonComponent button)
                return new ButtonComponentViewModel(button, this);

            if (component is DiscordLinkButtonComponent link)
                return new ButtonComponentViewModel(link, this);

            return new UnknownComponentViewModel(component, this);
        }

        // TODO: would an observable dictionary type be faster here?
        // probably.
        private void UpdateReactions() => syncContext.Post((o) =>
        {
            foreach (var reaction in Message.Reactions)
            {
                ReactionViewModel model;
                if ((model = Reactions.FirstOrDefault(r => r.Equals(reaction))) == null)
                    Reactions.Add(new ReactionViewModel(reaction, ReactCommand));
                else
                    model.Update(reaction);
            }

            foreach (var model in Reactions.ToList())
            {
                if (!Message.Reactions.Any(r => model.Emoji == r.Emoji))
                    Reactions.Remove(model);
            }
        }, null);
    }
}
