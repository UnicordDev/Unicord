using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.User;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands.Users
{
    public class ShowUserOverlayCommand : DiscordCommand<UserViewModel>
    {
        public ShowUserOverlayCommand(UserViewModel viewModel) : base(viewModel)
        {
        }

        public override async void Execute(object parameter)
        {
            // TODO: will probably be reworked
            await OverlayService.GetForCurrentView()
                .ShowOverlayAsync<UserInfoOverlayPage>(viewModel);
        }
    }
}
