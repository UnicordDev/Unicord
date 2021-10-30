using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands
{
    class CopyUrlCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordMessage || parameter is DiscordChannel || parameter is DiscordGuild || parameter is string;
        }

        public void Execute(object parameter)
        {
            var package = new DataPackage();

            if (parameter is DiscordMessage message)
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyMessageLink");
                var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
                package.SetText("https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}");
            }

            if (parameter is DiscordChannel channel)
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyChannelLink");
                var serverText = channel.Guild != null ? channel.GuildId.ToString() : "@me";
                package.SetText("https://" + $"discordapp.com/channels/{serverText}/{channel.Id}");
            }

            if (parameter is DiscordGuild guild)
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyGuildLink");
                package.SetText("https://" + $"discordapp.com/channels/{guild.Id}");
            }

            if (parameter is string str && Uri.TryCreate(str, UriKind.Absolute, out var uri))
            {
                package.SetText(str);
                package.SetWebLink(uri);
            }

            Clipboard.SetContent(package);
        }
    }
}
