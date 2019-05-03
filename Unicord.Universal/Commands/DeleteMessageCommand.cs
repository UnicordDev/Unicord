using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Dialogs;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Commands
{
    class DeleteMessageCommand : ICommand
    {
        public static readonly DeleteMessageCommand Instance
            = new DeleteMessageCommand();
        
        public event EventHandler CanExecuteChanged;

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

        public async void Execute(object parameter)
        {
            if(parameter is DiscordMessage message)
            {
                var dialog = new DeleteMessageDialog(message);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}
