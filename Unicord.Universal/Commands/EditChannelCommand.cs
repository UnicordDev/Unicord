using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Pages;
using Unicord.Universal.Pages.Management;
using Unicord.Universal.Services;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands
{
    class EditChannelCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordChannel channel ||
                (parameter is ChannelViewModel channelVM && (channel = channelVM.Channel) != null))
            {
                if (channel is DiscordDmChannel)
                    return false;

                var currentMember = channel.Guild.CurrentMember;
                if (currentMember.IsOwner)
                    return true;

                var permissions = currentMember.PermissionsIn(channel);
                if (permissions.HasFlag(Permissions.ManageChannels))
                {
                    return true;
                }
            }

            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel)
            {
                var page = Window.Current.Content.FindChild<DiscordPage>();
                if (page != null)
                {
                    Analytics.TrackEvent("EditChannelCommand_Invoked");
                    OverlayService.GetForCurrentView()
                        .ShowOverlayAsync<ChannelEditPage>(channel);
                }
            }
        }
    }
}
