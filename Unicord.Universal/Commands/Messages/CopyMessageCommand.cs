using System;
using System.Web;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Messages;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands.Messages
{
    public class CopyMessageCommand : DiscordCommand<MessageViewModel>
    {
        public CopyMessageCommand(MessageViewModel viewModel) : base(viewModel)
        {
        }

        public override void Execute(object parameter)
        {
            Analytics.TrackEvent("CopyMessageCommand_Invoked");
            var message = viewModel.Message;

            var package = new DataPackage();
            package.RequestedOperation = DataPackageOperation.Copy | DataPackageOperation.Link;

            var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
            var uri = "https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}/";

            package.SetText(message.Content);
            package.SetWebLink(new Uri(uri));
            package.SetRtf($"{{\\field{{\\*\\fldinst HYPERLINK \"{uri}\"}}{{\fldrslt {message.Content}}}");

            Clipboard.SetContent(package);
        }
    }
}
