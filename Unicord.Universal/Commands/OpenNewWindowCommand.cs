using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;

namespace Unicord.Universal.Commands
{
    class OpenNewWindowCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return (parameter is DiscordChannel channel) && channel.Type != ChannelType.Voice && WindowingService.Current.IsSupported;
        }

        public async void Execute(object parameter)
        {
            Analytics.TrackEvent("OpenNewWindowCommand_Invoked");

            if (parameter is DiscordChannel channel && channel.Type != ChannelType.Voice)
            {
                await WindowingService.Current.OpenChannelWindowAsync(channel);
            }
        }
    }
}
