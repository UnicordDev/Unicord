using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Unicord.Universal.Commands
{
    class MuteCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordChannel || parameter is DiscordGuild)
            {
                return true;
            }

            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel)
            {
                channel.Muted = !channel.Muted;
            }

            if (parameter is DiscordGuild guild)
            {
                guild.Muted = !guild.Muted;
            }
        }
    }
}
