using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;

namespace Unicord.Universal.Commands
{
    class MuteCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordChannel || parameter is DiscordGuild)
            {
                return true;
            }

            return true;
        }

        public void Execute(object parameter)
        {
            Analytics.TrackEvent("MuteCommand_Invoked");

            if (parameter is DiscordChannel channel)
            {
                channel.Muted = !channel.Muted;
            }

            if (parameter is DiscordGuild guild)
            {
                guild.Muted = !guild.Muted;
            }
        }
    }
}
