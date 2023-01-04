using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Models.Messaging
{
    /// <summary>
    /// A base message that signals whenever a specific value has changed.
    /// </summary>
    /// <typeparam name="T">The type of value that has changed.</typeparam>
    public class DiscordEventMessage<T> : CollectionRequestMessage<Task> where T : AsyncEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueChangedMessage{T}"/> class.
        /// </summary>
        /// <param name="value">The value that has changed.</param>
        public DiscordEventMessage(T value)
        {
            Event = value;
        }

        /// <summary>
        /// Gets the value that has changed.
        /// </summary>
        public T Event { get; }
    }
}
