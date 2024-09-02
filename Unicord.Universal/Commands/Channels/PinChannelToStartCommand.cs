using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Shared;
using Windows.UI.StartScreen;

namespace Unicord.Universal.Commands
{
    public class PinChannelToStartCommand : DiscordCommand<ChannelViewModel>
    {
        public PinChannelToStartCommand(ChannelViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return !SecondaryTile.Exists($"Channel_{viewModel.Id}");
        }

        public override async void Execute(object parameter)
        {
            Analytics.TrackEvent("PinChannelCommand_PinChannel");

            var channel = viewModel.Channel;
            var tile = new SecondaryTile(
                $"Channel_{channel.Id}",
                NotificationUtils.GetChannelHeaderName(channel),
                $"-channelId={channel.Id.ToString(CultureInfo.InvariantCulture)}",
                new Uri("ms-appx:///Assets/Store/Square150x150Logo.png"),
                TileSize.Square150x150);

            tile.VisualElements.ShowNameOnSquare150x150Logo = true;
            tile.VisualElements.ShowNameOnWide310x150Logo = true;
            tile.VisualElements.ShowNameOnSquare310x310Logo = true;

            if (await tile.RequestCreateAsync())
            {
                SecondaryTileManager.ClearTileNotifications(channel, tile);
            }
        }
    }
}
