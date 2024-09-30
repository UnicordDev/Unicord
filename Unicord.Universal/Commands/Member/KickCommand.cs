using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Services;
using Unicord.Universal.Models.User;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands.Members
{
    public class KickCommand : DiscordCommand<UserViewModel>
    {
        public KickCommand(UserViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var member = viewModel.Member;
            if (member == null)
                return false;

            if (member.Id == DiscordManager.Discord.CurrentUser.Id)
                return false;

            if (!Tools.CheckRoleHierarchy(member.Guild.CurrentMember, member))
                return false;

            return member.Guild.CurrentMember.PermissionsIn(null).HasPermission(Permissions.KickMembers);
        }

        public override async void Execute(object parameter)
        {
            Analytics.TrackEvent("KickCommand_Invoked");

            var kickDialog = new KickDialog(viewModel.Member);
            var result = await kickDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Analytics.TrackEvent("KickCommand_KickMember");
                await viewModel.Member.RemoveAsync(kickDialog.KickReason);
            }
        }
    }
}
