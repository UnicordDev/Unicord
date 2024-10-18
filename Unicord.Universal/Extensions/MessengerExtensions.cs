using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.AsyncEvents;
using Unicord.Universal.Models.Messaging;

namespace Unicord.Universal.Extensions
{
    internal static class MessengerExtensions
    {
        public delegate Task AsyncMessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
            where TRecipient : class where TMessage : class;

        /// <summary>
        /// Registers a recipient for a given type of message.
        /// </summary>
        /// <typeparam name="TRecipient">The type of recipient for the message.</typeparam>
        /// <typeparam name="TMessage">The type of message to receive.</typeparam>
        /// <param name="messenger">The <see cref="IMessenger"/> instance to use to register the recipient.</param>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="handler">The <see cref="MessageHandler{TRecipient,TMessage}"/> to invoke when a message is received.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
        /// <remarks>This method will use the default channel to perform the requested registration.</remarks>
        public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, DiscordEventMessage<TMessage>> handler)
            where TRecipient : class
            where TMessage : AsyncEventArgs
        {
            messenger.Register<TRecipient, DiscordEventMessage<TMessage>>(recipient, handler);
        }

        /// <summary>
        /// Registers a recipient for a given type of message.
        /// </summary>
        /// <typeparam name="TRecipient">The type of recipient for the message.</typeparam>
        /// <typeparam name="TMessage">The type of message to receive.</typeparam>
        /// <param name="messenger">The <see cref="IMessenger"/> instance to use to register the recipient.</param>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="handler">The <see cref="MessageHandler{TRecipient,TMessage}"/> to invoke when a message is received.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
        /// <remarks>This method will use the default channel to perform the requested registration.</remarks>
        public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, AsyncMessageHandler<TRecipient, DiscordEventMessage<TMessage>> handler)
            where TRecipient : class
            where TMessage : AsyncEventArgs
        {
            messenger.Register<TRecipient, DiscordEventMessage<TMessage>>(recipient, (t, v) => v.Reply(handler(t, v)));
        }

        /// <summary>
        /// Sends a message of the specified type to all registered recipients.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to send.</typeparam>
        /// <param name="messenger">The <see cref="IMessenger"/> instance to use to send the message.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message that was sent (ie. <paramref name="message"/>).</returns>
        public static DiscordEventMessage<TMessage> Send<TMessage>(this IMessenger messenger, TMessage message)
            where TMessage : AsyncEventArgs
        {
            return messenger.Send(new DiscordEventMessage<TMessage>(message));
        }
    }
}