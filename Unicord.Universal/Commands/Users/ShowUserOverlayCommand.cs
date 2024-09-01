using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands.Users
{
    public class ShowUserOverlayCommand : DiscordCommand<UserViewModel>
    {
        public ShowUserOverlayCommand(UserViewModel viewModel) : base(viewModel)
        {
        }

        public override void Execute(object parameter)
        {
            // TODO: will probably be reworked
            var page = Window.Current.Content.FindChild<MainPage>();
            if (page != null)
            {
                Analytics.TrackEvent("ShowUserOverlayCommand_Invoked");
                page.ShowUserOverlay(viewModel.User, true);
            }
        }
    }
}
