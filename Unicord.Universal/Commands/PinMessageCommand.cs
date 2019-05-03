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
    class PinMessageCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMessage message)
            {
                if (message.Channel is DiscordDmChannel)
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
            if (parameter is DiscordMessage message)
            {
                var dialog = new PinMessageDialog(message);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    if (message.Pinned)
                    {
                        await message.UnpinAsync();
                    }
                    else
                    {
                        await message.PinAsync();
                    }
                }
            }
        }
    }
}
