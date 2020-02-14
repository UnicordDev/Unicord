using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands
{
    class CopyMessageCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordMessage;
        }

        public void Execute(object parameter)
        {
            var package = new DataPackage();

            if (parameter is DiscordMessage message)
            {
                var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
                package.SetText(message.Content);
                // MAYBE: package.SetHtmlFormat()
                package.SetWebLink(new Uri("https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}/"));
            }

            Clipboard.SetContent(package);
        }
    }
}
