using System;
using System.Linq;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Dialogs;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    class ChangeNicknameCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                var permissions = member.Guild.CurrentMember.PermissionsIn(null);
                if (member.IsCurrent && (permissions.HasFlag(Permissions.ChangeNickname) || permissions.HasFlag(Permissions.ManageNicknames)))
                {
                    return true;
                }

                if (permissions.HasFlag(Permissions.ManageNicknames) && Tools.CheckRoleHierarchy(member.Guild.CurrentMember, member))
                {
                    return true;
                }
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                var dialog = new ChangeNicknameDialog(member);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await member.ModifyAsync(m => m.Nickname = dialog.Text);
                }
            }
        }
    }
}
