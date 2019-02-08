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

namespace Unicord.Universal.Commands
{
    class ChangeNicknameCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                var channel = member.Guild.Channels.FirstOrDefault();
                if (channel != null)
                {
                    var permissions = member.Guild.CurrentMember.PermissionsIn(channel);
                    if (member.IsCurrent && (permissions.HasFlag(Permissions.ChangeNickname) || permissions.HasFlag(Permissions.ManageNicknames)))
                    {
                        return true;
                    }

                    if (permissions.HasFlag(Permissions.ManageNicknames) && Tools.CheckRoleHeirarchy(member, member.Guild.CurrentMember))
                    {
                        return true;
                    }
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
