using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    class BanCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                if (member.IsCurrent)
                    return false;

                if (!Tools.CheckRoleHierarchy(member.Guild.CurrentMember, member))
                    return false;

                return member.Guild.CurrentMember.PermissionsIn(null).HasPermission(Permissions.BanMembers);
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                var banDialog = new BanDialog(member);
                var result = await banDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                    await member.BanAsync(banDialog.DeleteMessageDays, banDialog.BanReason);
            }
        }
    }
}
