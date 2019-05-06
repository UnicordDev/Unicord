using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;

namespace Unicord.Universal.Commands
{
    class MuteCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordChannel channel)
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
        }
    }
}
