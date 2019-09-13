using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.UICommands
{
    class CopyUrlCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordMessage || parameter is DiscordChannel || parameter is DiscordGuild;
        }

        public void Execute(object parameter)
        {
            var package = new DataPackage();

            if (parameter is DiscordMessage message)
            {
                var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
                package.SetText("https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}/");
            }

            if (parameter is DiscordChannel channel)
            {
                var serverText = channel.Guild != null ? channel.GuildId.ToString() : "@me";
                package.SetText("https://" + $"discordapp.com/channels/{serverText}/{channel.Id}/");
            }

            if (parameter is DiscordGuild guild)
            {
                package.SetText("https://" + $"discordapp.com/channels/{guild.Id}/");
            }

            Clipboard.SetContent(package);
        }
    }
}
