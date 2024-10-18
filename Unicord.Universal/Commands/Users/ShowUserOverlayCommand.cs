using Unicord.Universal.Models.User;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;

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
