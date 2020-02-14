using System;
using System.Windows.Input;
using DSharpPlus.Entities;

namespace Unicord.Universal.Commands
{
    class AcknowledgeCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordChannel c && c.ReadState?.Unread == true || parameter is DiscordGuild guild && guild.Unread;
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel && channel.ReadState?.Unread == true)
            {
                var message = await channel.GetMessageAsync(channel.LastMessageId);
                if (message != null)
                    await message.AcknowledgeAsync();
            }

            if (parameter is DiscordGuild guild && guild.Unread)
            {
                await guild.AcknowledgeAsync();
            }
        }
    }
}
