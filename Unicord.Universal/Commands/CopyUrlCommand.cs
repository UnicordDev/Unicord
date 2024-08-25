using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.Messages;
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
            return parameter is DiscordMessage or DiscordChannel or DiscordGuild or string or MessageViewModel or ChannelViewModel or GuildViewModel;
        }

        public void Execute(object parameter)
        {
            var package = new DataPackage();

            if (parameter is DiscordMessage message || 
                (parameter is MessageViewModel messageVM && (message = messageVM.Message) != null))
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyMessageLink");
                var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
                var url = "https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}";
                package.SetText(url);
                package.SetWebLink(new Uri(url));
            }

            if (parameter is DiscordChannel channel || 
                (parameter is ChannelViewModel channelVM && (channel = channelVM.Channel) != null))
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyChannelLink");
                var serverText = channel.Guild != null ? channel.GuildId.ToString() : "@me";
                var url = "https://" + $"discordapp.com/channels/{serverText}/{channel.Id}";
                package.SetText(url);
                package.SetWebLink(new Uri(url));
            }

            if (parameter is DiscordGuild guild || 
                (parameter is GuildViewModel guildVM && (guild = guildVM.Guild) != null))
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyGuildLink");
                var url = "https://" + $"discordapp.com/channels/{guild.Id}";
                package.SetText(url);
                package.SetWebLink(new Uri(url));
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
