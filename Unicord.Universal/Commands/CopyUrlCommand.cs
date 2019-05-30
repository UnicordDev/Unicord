using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands
{
    class CopyUrlCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

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
