using System;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Commands
{
    class DeleteMessageCommand : ICommand
    {
        public static readonly DeleteMessageCommand Instance
            = new DeleteMessageCommand();

#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMessage message || 
                (parameter is MessageViewModel messageVM && (message = messageVM.Message) != null))
            {
                if (message.Author.Id == App.Discord.CurrentUser.Id)
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
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            Analytics.TrackEvent("DeleteMessageCommand_Invoked");

            if (parameter is DiscordMessage message)
            {
                var dialog = new DeleteMessageDialog() { Message = message };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    Analytics.TrackEvent("DeleteMessageCommand_DeleteMessage");
                    await message.DeleteAsync();
                }
            }
        }
    }
}
