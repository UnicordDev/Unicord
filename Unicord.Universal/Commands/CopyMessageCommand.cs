using System;
using System.Web;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Messages;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands
{
    class CopyMessageCommand : ICommand
    {
        public CopyMessageCommand()
        {

        }

#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordMessage or MessageViewModel;
        }

        public void Execute(object parameter)
        {
            if (parameter is not DiscordMessage message)
            {
                if (parameter is MessageViewModel vm && vm.Message != null)
                    message = vm.Message;
                else
                    return;
            }

            Analytics.TrackEvent("CopyMessageCommand_Invoked");

            var package = new DataPackage();
            package.RequestedOperation = DataPackageOperation.Copy | DataPackageOperation.Link;

            var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
            var uri = "https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}/";
            //var markdown = Formatter.MaskedUrl(message.Content, new Uri(uri));

            package.SetText(message.Content);
            package.SetWebLink(new Uri(uri));
            package.SetRtf($"{{\\field{{\\*\\fldinst HYPERLINK \"{uri}\"}}{{\fldrslt {message.Content}}}");

            Clipboard.SetContent(package);
        }
    }
}
