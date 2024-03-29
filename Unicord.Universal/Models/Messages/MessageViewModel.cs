﻿using DSharpPlus;
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
using Unicord.Universal.Commands.Messages;
using Unicord.Universal.Models.Messaging;

namespace Unicord.Universal.Models.Messages
{
    public class MessageViewModel : ViewModelBase
    {
        public MessageViewModel(DiscordMessage discordMessage, ChannelViewModel parent = null)
        {
            Message = discordMessage;
            Parent = parent;

            ReactCommand = new ReactCommand(discordMessage);
            Embeds = new ObservableCollection<DiscordEmbed>(Message.Embeds);
            Attachments = new ObservableCollection<AttachmentViewModel>(Message.Attachments.Select(a => new AttachmentViewModel(a)));
            Stickers = new ObservableCollection<DiscordSticker>(Message.Stickers);
            Components = new ObservableCollection<DiscordComponent>(Message.Components);
            Reactions = new ObservableCollection<ReactionViewModel>(Message.Reactions.Select(r => new ReactionViewModel(r, ReactCommand)));

            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageUpdateEventArgs>(this, (t, e) => t.OnMessageUpdated(e.Event));
            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionAddEventArgs>(this, (t, e) => t.OnReactionAdded(e.Event));
            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionRemoveEventArgs>(this, (t, e) => t.OnReactionRemoved(e.Event));
            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionRemoveEmojiEventArgs>(this, (t, e) => t.OnReactionGroupRemoved(e.Event));
            WeakReferenceMessenger.Default.Register<MessageViewModel, MessageReactionsClearEventArgs>(this, (t, e) => t.OnReactionsCleared(e.Event));

            if (parent != null && parent.Channel.Guild != null)
            {
                WeakReferenceMessenger.Default.Register<MessageViewModel, DiscordEventMessage<GuildMemberUpdateEventArgs>>(this,
                    (t, e) => t.OnGuildMemberUpdate(e.Event));
            }
        }

        public DiscordMessage Message { get; }

        public ChannelViewModel Parent { get; }

        public ulong Id
            => Message.Id;
        public DiscordMessage ReferencedMessage
            => Message.ReferencedMessage;
        public DiscordChannel Channel
            => Message.Channel;
        public DiscordUser Author
            => Message.Author;
        public DateTimeOffset Timestamp
            => Message.Timestamp;
        public string Content
            => Message.Content;
        public bool IsEdited
            => Message.IsEdited;
        public bool IsSystemMessage
            => Message.MessageType != MessageType.Default && Message.MessageType != MessageType.Reply;

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
                        if (other.Author.Id == Message.Author.Id && timeSpan <= TimeSpan.FromMinutes(10))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public ObservableCollection<DiscordEmbed> Embeds { get; }
        public ObservableCollection<AttachmentViewModel> Attachments { get; }
        public ObservableCollection<DiscordSticker> Stickers { get; }
        public ObservableCollection<DiscordComponent> Components { get; }

        // TODO: Move the above to individual view models
        public ObservableCollection<ReactionViewModel> Reactions { get; }

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

            if (e.MessageBefore.Content != e.Message.Content)
                InvokePropertyChanged(nameof(Content));

            if (e.MessageBefore.IsEdited != e.Message.IsEdited)
                InvokePropertyChanged(nameof(IsEdited));

            if (Embeds.SequenceEqual(e.Message.Embeds))
                return;

            syncContext.Post((o) =>
            {
                Embeds.Clear();
                foreach (var embed in e.Message.Embeds)
                    Embeds.Add(embed);

            }, null);

            return;
        }

        private void OnGuildMemberUpdate(GuildMemberUpdateEventArgs e)
        {
            if (e.Member.Id != Author.Id)
                return;

            if (e.Guild.Id != Channel.GuildId)
                return;

            if (Author is not DiscordMember member && !e.Guild.GetCachedMember(Author.Id, out member))
                return;

            InvokePropertyChanged(nameof(Author));
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
                    model.InvokePropertyChanged("");
            }

            foreach (var model in Reactions.ToList())
            {
                if (!Message.Reactions.Any(r => r.Emoji == model.Emoji))
                    Reactions.Remove(model);
            }
        }, null);
    }
}
