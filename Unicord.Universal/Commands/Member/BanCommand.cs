using System;
using DSharpPlus;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands.Members
{
    public class BanCommand : DiscordCommand<UserViewModel>
    {
        public BanCommand(UserViewModel viewModel)
            : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            if (viewModel.Member == null) return false;
            
            var member = viewModel.Member;

            if (member.Id == discord.CurrentUser.Id)
                return false;

            if (!Tools.CheckRoleHierarchy(member.Guild.CurrentMember, member))
                return false;

            return member.Guild.CurrentMember.PermissionsIn(null).HasPermission(Permissions.BanMembers);

        }

        public override async void Execute(object parameter)
        {
            Analytics.TrackEvent("BanCommand_Invoked");

            var banDialog = new BanDialog(viewModel.Member);
            var result = await banDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Analytics.TrackEvent("BanCommand_BanMember");
                await viewModel.Member.BanAsync(banDialog.DeleteMessageDays, banDialog.BanReason);
            }
        }
    }
}
