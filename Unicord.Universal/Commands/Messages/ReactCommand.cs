using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;

namespace Unicord.Universal.Commands.Messages
{
    internal class ReactCommand : ICommand
    {
        private DiscordMessage _message;

        public ReactCommand(DiscordMessage message)
        {
            _message = message;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordEmoji;
        }

        public async void Execute(object parameter)
        {
            if (parameter is not DiscordEmoji emoji)
                return;

            if (_message.Reactions.Any(r => r.IsMe && r.Emoji == emoji))
            {
                await _message.DeleteOwnReactionAsync(emoji);
            }
            else
            {
                await _message.CreateReactionAsync(emoji);
            }
        }
    }
}
