using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands.Messages
{
    public class DeleteMessageCommand : DiscordCommand<MessageViewModel>
    {
        public DeleteMessageCommand(MessageViewModel viewModel) : base(viewModel) { }

        public override bool CanExecute(object parameter)
        {
            var message = viewModel.Message;
            if (message.Author.IsCurrent)
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

                if (currentMember.PermissionsIn(message.Channel).HasPermission(Permissions.ManageMessages))
                {
                    return true;
                }
            }

            return false;
        }

        public override async void Execute(object parameter)
        {
            Analytics.TrackEvent("DeleteMessageCommand_Invoked");
            var message = viewModel.Message;

            var dialog = new DeleteMessageDialog() { Message = message };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Analytics.TrackEvent("DeleteMessageCommand_DeleteMessage");
                await message.DeleteAsync();
            }
        }
    }
}
