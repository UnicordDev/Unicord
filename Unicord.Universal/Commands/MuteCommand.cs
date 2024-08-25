using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Commands
{
    class MuteCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            //if (parameter is DiscordChannel || parameter is DiscordGuild)
            //{
            //    return true;
            //}

            //return true;

            return parameter is DiscordChannel or DiscordGuild or ChannelViewModel or GuildViewModel;
        }

        public async void Execute(object parameter)
        {
            Analytics.TrackEvent("MuteCommand_Invoked");

            try
            {
                if (parameter is DiscordChannel channel)
                {
                    // if (!channel.Muted)
                    //    await channel.MuteAsync();
                }

                if (parameter is DiscordGuild guild)
                {
                    // guild.Muted = !guild.Muted;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}
