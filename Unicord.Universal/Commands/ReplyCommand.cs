using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands
{
    class ReplyCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return (parameter is DiscordMessage message && (message.MessageType == MessageType.Default || message.MessageType == MessageType.Reply)) || parameter is null;
        }

        public void Execute(object parameter)
        {
            Analytics.TrackEvent("ReplyCommand_Invoked");

            var channelPage = Window.Current.Content.FindChild<ChannelPage>();
            if (channelPage == null)
                return;

            if (parameter is DiscordMessage message)
            {
                if (channelPage?.ViewModel != null)
                    channelPage.ViewModel.ReplyTo = message;
            }

            if (parameter is null)
            {
                if (channelPage?.ViewModel != null)
                    channelPage.ViewModel.ReplyTo = null;
            }

            channelPage.FocusTextBox();
        }
    }
}
