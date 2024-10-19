using System;
using System.Windows.Input;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.Messages;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands.Generic
{
    public class CopyUrlCommand : ICommand
    {
        private readonly ViewModelBase viewModel;
        public CopyUrlCommand(ViewModelBase viewModel)
        {
            this.viewModel = viewModel;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var package = new DataPackage();

            if (viewModel is MessageViewModel message)
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyMessageLink");
                var serverText = message.Channel.Guild != null ? message.Channel.Guild.Id.ToString() : "@me";
                var url = "https://" + $"discordapp.com/channels/{serverText}/{message.Channel.Id}/{message.Id}";
                package.SetText(url);
                package.SetWebLink(new Uri(url));
            }

            if (viewModel is ChannelViewModel channel)
            {
                Analytics.TrackEvent("CopyUrlCommand_CopyChannelLink");
                var serverText = channel.Guild != null ? channel.Guild.Id.ToString() : "@me";
                var url = "https://" + $"discordapp.com/channels/{serverText}/{channel.Id}";
                package.SetText(url);
                package.SetWebLink(new Uri(url));
            }

            if (viewModel is GuildViewModel guild)
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
