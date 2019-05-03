using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Pages;
using Unicord.Universal.Pages.Management;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    class EditChannelCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordChannel channel)
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
                    page.OpenCustomPane(typeof(ChannelEditPage), channel);
                }
            }
        }
    }
}
