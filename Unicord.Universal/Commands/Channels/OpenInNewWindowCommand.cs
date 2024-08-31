using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;

namespace Unicord.Universal.Commands.Channels
{
    internal class OpenInNewWindowCommand : ICommand
    {
        private readonly WindowHandle windowHandle;
        private readonly bool compactOverlay;

        public OpenInNewWindowCommand(WindowHandle windowHandle, bool compactOverlay)
        {
            this.windowHandle = windowHandle;
            this.compactOverlay = compactOverlay;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return WindowingService.Current.IsSupported &&
                   WindowingService.Current.IsMainWindow(windowHandle) &&
                   (parameter is DiscordChannel channel || (parameter is ChannelViewModel channelVm && (channel = channelVm.Channel) != null)) &&
                   channel.IsText();
        }

        public async void Execute(object parameter)
        {
            if (parameter is not DiscordChannel channel &&
                (parameter is not ChannelViewModel channelVm || (channel = channelVm.Channel) == null) || !channel.IsText())
                return;

            var newHandle = await WindowingService.Current.OpenChannelWindowAsync(channel, compactOverlay, windowHandle);
            if (newHandle != null)
            {
                WindowingService.Current.SetWindowChannel(windowHandle, 0);
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(null, true);
            }
        }
    }
}
