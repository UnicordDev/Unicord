﻿using System.Linq;
using Unicord.Universal.Models.User;
using Unicord.Universal.Services;

namespace Unicord.Universal.Commands.Users
{
    internal class SendMessageCommand : DiscordCommand<UserViewModel>
    {
        public SendMessageCommand(UserViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var channel = discord.PrivateChannels.Values
                .Where(c => c.IsPrivate && c.Recipients.Count > 0)
                .FirstOrDefault(c => c.Recipients[0].Id == viewModel.Id);

            return channel != null;
        }

        public override async void Execute(object parameter)
        {
            var channel = discord.PrivateChannels.Values
                .Where(c => c.IsPrivate && c.Recipients.Count > 0)
                .FirstOrDefault(c => c.Recipients[0].Id == viewModel.Id);

            var service = DiscordNavigationService.GetForCurrentView();
            await service.NavigateAsync(channel);
        }
    }
}
