using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Unicord.Universal.Extensions;
// ?? why do i need this suddenly
using WeakReferenceMessenger = CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

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
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnReady(DiscordClient client, ReadyEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnResumed(DiscordClient client, ResumedEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnUserSettingsUpdated(DiscordClient client, UserSettingsUpdateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageAcknowledged(DiscordClient client, MessageAcknowledgeEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageReactionRemovedEmoji(DiscordClient client, MessageReactionRemoveEmojiEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnMessageReactionsCleared(DiscordClient client, MessageReactionsClearEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnTypingStarted(DiscordClient client, TypingStartEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnChannelCreated(DiscordClient client, ChannelCreateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnChannelUpdated(DiscordClient client, ChannelUpdateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnChannelDeleted(DiscordClient client, ChannelDeleteEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnGuildCreated(DiscordClient client, GuildCreateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnGuildDeleted(DiscordClient client, GuildDeleteEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnGuildUpdated(DiscordClient client, GuildUpdateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnGuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnDmChannelCreated(DiscordClient client, DmChannelCreateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnDmChannelDeleted(DiscordClient client, DmChannelDeleteEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnRelationshipAdded(DiscordClient client, RelationshipAddEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnRelationshipRemoved(DiscordClient client, RelationshipRemoveEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnPresenceUpdated(DiscordClient client, PresenceUpdateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnReadStateUpdated(DiscordClient client, ReadStateUpdateEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnGuildMembersChunked(DiscordClient client, GuildMembersChunkEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnChannelUnreadUpdated(DiscordClient client, ChannelUnreadUpdateEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnSocketOpened(DiscordClient sender, SocketEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs e)
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));

        private static Task OnSocketClosed(DiscordClient sender, SocketCloseEventArgs e) 
            => Task.WhenAll(WeakReferenceMessenger.Default.Send(e));
    }
}
