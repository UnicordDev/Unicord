using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Shared;
using Windows.UI.StartScreen;

namespace Unicord.Universal.Commands
{
    public class PinChannelCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordChannel channel && !SecondaryTile.Exists($"Channel_{channel.Id}");
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel && !SecondaryTile.Exists($"Channel_{channel.Id}"))
            {
                Analytics.TrackEvent("PinChannelCommand_PinChannel");

                var tile = new SecondaryTile(
                    $"Channel_{channel.Id}",
                    NotificationUtils.GetChannelHeaderName(channel),
                    $"-channelId={channel.Id}",
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
}
