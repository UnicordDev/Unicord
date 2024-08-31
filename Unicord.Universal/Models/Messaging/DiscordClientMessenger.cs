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
            client.SocketOpened += OnSocketOpened;
            client.SocketErrored += OnSocketErrored;
            client.SocketClosed += OnSocketClosed;
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
            client.ChannelUnreadUpdated += OnChannelUnreadUpdated;
            client.TypingStarted += OnTypingStarted;
            client.GuildCreated += OnGuildCreated;
            client.GuildDeleted += OnGuildDeleted;
            client.GuildUpdated += OnGuildUpdated;
            client.GuildMemberUpdated += OnGuildMemberUpdated;
            client.GuildMembersChunked += OnGuildMembersChunked;
            client.DmChannelCreated += OnDmChannelCreated;
            client.DmChannelDeleted += OnDmChannelDeleted;
            client.RelationshipAdded += OnRelationshipAdded;
            client.RelationshipRemoved += OnRelationshipRemoved;
            client.PresenceUpdated += OnPresenceUpdated;
            client.ReadStateUpdated += OnReadStateUpdated;
        }
        
        public static void Unregister(DiscordClient client)
        {
            client.Ready -= OnReady;
            client.Resumed -= OnResumed;
            client.UserUpdated -= OnUserUpdated;
            client.UserSettingsUpdated -= OnUserSettingsUpdated;
            client.MessageCreated -= OnMessageCreated;
            client.MessageDeleted -= OnMessageDeleted;
            client.MessageUpdated -= OnMessageUpdated;
            client.MessageAcknowledged -= OnMessageAcknowledged;
            client.MessageReactionAdded -= OnMessageReactionAdded;
            client.MessageReactionRemoved -= OnMessageReactionRemoved;
            client.MessageReactionRemovedEmoji -= OnMessageReactionRemovedEmoji;
            client.MessageReactionsCleared -= OnMessageReactionsCleared;
            client.ChannelCreated -= OnChannelCreated;
            client.ChannelDeleted -= OnChannelDeleted;
            client.ChannelUpdated -= OnChannelUpdated;
            client.ChannelUnreadUpdated -= OnChannelUnreadUpdated;
            client.TypingStarted -= OnTypingStarted;
            client.GuildCreated -= OnGuildCreated;
            client.GuildDeleted -= OnGuildDeleted;
            client.GuildUpdated -= OnGuildUpdated;
            client.GuildMemberUpdated -= OnGuildMemberUpdated;
            client.GuildMembersChunked -= OnGuildMembersChunked;
            client.DmChannelCreated -= OnDmChannelCreated;
            client.DmChannelDeleted -= OnDmChannelDeleted;
            client.RelationshipAdded -= OnRelationshipAdded;
            client.RelationshipRemoved -= OnRelationshipRemoved;
            client.PresenceUpdated -= OnPresenceUpdated;
            client.ReadStateUpdated -= OnReadStateUpdated;
        }

        private static Task OnUserUpdated(DiscordClient client, UserUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnResumed(DiscordClient client, ResumedEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnUserSettingsUpdated(DiscordClient client, UserSettingsUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }
        
        private static Task OnMessageAcknowledged(DiscordClient client, MessageAcknowledgeEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionRemovedEmoji(DiscordClient client, MessageReactionRemoveEmojiEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnMessageReactionsCleared(DiscordClient client, MessageReactionsClearEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnTypingStarted(DiscordClient client, TypingStartEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelCreated(DiscordClient client, ChannelCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelUpdated(DiscordClient client, ChannelUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelDeleted(DiscordClient client, ChannelDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildCreated(DiscordClient client, GuildCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildUpdated(DiscordClient client, GuildUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnDmChannelCreated(DiscordClient client, DmChannelCreateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnDmChannelDeleted(DiscordClient client, DmChannelDeleteEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnRelationshipAdded(DiscordClient client, RelationshipAddEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnRelationshipRemoved(DiscordClient client, RelationshipRemoveEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnPresenceUpdated(DiscordClient client, PresenceUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnReadStateUpdated(DiscordClient client, ReadStateUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnGuildMembersChunked(DiscordClient client, GuildMembersChunkEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnChannelUnreadUpdated(DiscordClient client, ChannelUnreadUpdateEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnSocketOpened(DiscordClient sender, SocketEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }

        private static Task OnSocketClosed(DiscordClient sender, SocketCloseEventArgs e)
        {
            return Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
        }
    }
}
