using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using System;
using System.Windows.Input;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    public class PinMessageCommand : DiscordCommand<MessageViewModel>
    {
        public PinMessageCommand(MessageViewModel viewModel) : base(viewModel) { }

        public override bool CanExecute(object parameter)
        {
            var message = viewModel.Message;

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

            return false;
        }

        public override async void Execute(object parameter)
        {
            Analytics.TrackEvent("PinMessageCommand_Invoked");

            var message = viewModel.Message;
            var dialog = new PinMessageDialog() { Message = message };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Analytics.TrackEvent("PinMessageCommand_PinMessage");

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
