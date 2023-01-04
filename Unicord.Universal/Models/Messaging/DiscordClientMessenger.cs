using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Models.Messaging
{
    internal class DiscordClientMessenger
    {
        public static void Register(DiscordClient client)
        {
            client.Ready += OnReady;
            client.Resumed += OnResumed;
            client.UserSettingsUpdated += OnUserSettingsUpdated;
            client.MessageCreated += OnMessageCreated;
            client.MessageDeleted += OnMessageDeleted;
            client.MessageUpdated += OnMessageUpdated;
            client.MessageAcknowledged += OnMessageAcknowledged;
            client.MessageReactionAdded += OnMessageReactionAdded;
            client.MessageReactionRemoved += OnMessageReactionRemoved;
            client.MessageReactionRemovedEmoji += OnMessageReactionRemovedEmoji;
            client.MessageReactionsCleared += OnMessageReactionsCleared;
            client.ChannelCreated += OnChannelCreated;
            client.ChannelDeleted += OnChannelDeleted;
            client.ChannelUpdated += OnChannelUpdated;
            client.TypingStarted += OnTypingStarted;
            client.GuildCreated += OnGuildCreated;
            client.GuildDeleted += OnGuildDeleted;
            client.GuildUpdated += OnGuildUpdated;
            client.GuildMemberUpdated += OnGuildMemberUpdated;
            client.DmChannelCreated += OnDmChannelCreated;
            client.DmChannelDeleted += OnDmChannelDeleted;
            client.RelationshipAdded += OnRelationshipAdded;
            client.RelationshipRemoved += OnRelationshipRemoved;
            client.PresenceUpdated += OnPresenceUpdated;
        }

        private static Task OnReady(ReadyEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnResumed(ResumedEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnUserSettingsUpdated(UserSettingsUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageCreated(MessageCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageDeleted(MessageDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageUpdated(MessageUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }
        
        private static Task OnMessageAcknowledged(MessageAcknowledgeEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionAdded(MessageReactionAddEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionRemovedEmoji(MessageReactionRemoveEmojiEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionsCleared(MessageReactionsClearEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnTypingStarted(TypingStartEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelCreated(ChannelCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelDeleted(ChannelDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildCreated(GuildCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildUpdated(GuildUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnDmChannelCreated(DmChannelCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnDmChannelDeleted(DmChannelDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnRelationshipAdded(RelationshipAddedEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnRelationshipRemoved(RelationshipRemovedEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnPresenceUpdated(PresenceUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }
    }
}
