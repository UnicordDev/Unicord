using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Unicord.Universal.Commands
{
    class AcknowledgeCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordChannel c && c.ReadState?.Unread == true || parameter is DiscordGuild guild && guild.Unread;
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel && channel.ReadState?.Unread == true)
            {
                var message = await channel.GetMessageAsync(channel.LastMessageId, true);
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
