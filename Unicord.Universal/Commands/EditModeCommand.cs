using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    class EditModeCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMessage message)
            {
                if (message.Author.Id == App.Discord.CurrentUser.Id)
                {
                    return true;
                }

                if (message.Author is DiscordMember member)
                {
                    var currentMember = member.Guild.CurrentMember;

                    if (currentMember.IsOwner)
                    {
                        return true;
                    }

                    if (currentMember.PermissionsIn(message.Channel).HasFlag(Permissions.ManageMessages))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is DiscordMessage message)
            {
                var channelPage = Window.Current.Content.FindChild<ChannelPage>();
                channelPage.EnterEditMode(message);
            }
        }
    }
}
