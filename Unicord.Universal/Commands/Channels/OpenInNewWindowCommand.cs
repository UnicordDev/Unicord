using System;
using System.Windows.Input;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;

namespace Unicord.Universal.Commands.Channels
{
    internal class OpenInNewWindowCommand : ICommand
    {
        private readonly ChannelViewModel channelViewModel;
        private readonly bool compactOverlay;

        public OpenInNewWindowCommand(ChannelViewModel channelViewModel, bool compactOverlay)
        {
            this.channelViewModel = channelViewModel;
            this.compactOverlay = compactOverlay;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return WindowingService.Current.IsSupported &&
                   WindowingService.Current.IsMainWindow(WindowingService.Current.CurrentWindow) &&
                   channelViewModel.Channel.IsText();
        }

        public async void Execute(object parameter)
        {
            if (!channelViewModel.Channel.IsText())
                return;

            var newHandle = await WindowingService.Current.OpenChannelWindowAsync(channelViewModel.Channel, compactOverlay, WindowingService.Current.CurrentWindow);
            if (newHandle != null)
            {
                WindowingService.Current.SetWindowChannel(WindowingService.Current.CurrentWindow, 0);
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync();
            }
        }
    }
}
