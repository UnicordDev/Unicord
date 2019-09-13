using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Dialogs;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.UICommands
{
    class PinMessageCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

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
