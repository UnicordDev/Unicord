using System;
using System.Linq;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands.Members
{
    class ChangeNicknameCommand : DiscordCommand<UserViewModel>
    {
        public ChangeNicknameCommand(UserViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            if (viewModel.Member == null) return false;

            var member = viewModel.Member;
            var permissions = member.Guild.CurrentMember.PermissionsIn(null);
            if (member.Id == App.Discord.CurrentUser.Id &&
                (permissions.HasFlag(Permissions.ChangeNickname) || permissions.HasFlag(Permissions.ManageNicknames)))
            {
                return true;
            }

            if (permissions.HasFlag(Permissions.ManageNicknames)
                && Tools.CheckRoleHierarchy(member.Guild.CurrentMember, member))
            {
                return true;
            }

            return false;
        }

        public override async void Execute(object parameter)
        {
            if (viewModel.Member == null) 
                return;

            var member = viewModel.Member;
            Analytics.TrackEvent("ChangeNicknameCommand_Invoked");

            var dialog = new ChangeNicknameDialog(member);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Analytics.TrackEvent("ChangeNicknameCommand_ChangeNickname");
                await member.ModifyAsync(m => m.Nickname = dialog.Text);
            }
        }
    }
}
