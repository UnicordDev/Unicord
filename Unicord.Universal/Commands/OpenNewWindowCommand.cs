using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Utilities;

namespace Unicord.Universal.Commands
{
    class OpenNewWindowCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return (parameter is DiscordChannel);
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel)
            {
                await WindowManager.OpenChannelWindowAsync(channel);
            }
        }
    }
}
