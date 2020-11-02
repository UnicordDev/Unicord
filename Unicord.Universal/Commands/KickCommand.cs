using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    public class KickCommand : ICommand
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

                return member.Guild.CurrentMember.PermissionsIn(null).HasPermission(Permissions.KickMembers);
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            Analytics.TrackEvent("KickCommand_Invoked");

            if (parameter is DiscordMember member)
            {
                var kickDialog = new KickDialog(member);
                var result = await kickDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    Analytics.TrackEvent("KickCommand_KickMember");
                    await member.RemoveAsync(kickDialog.KickReason);
                }
            }
        }
    }
}
