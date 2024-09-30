using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace Unicord.Universal.Shared
{
    internal class SecondaryTileManager
    {
        private readonly DiscordClient _discord = null;
        private readonly ConcurrentDictionary<ulong, SecondaryTile> _tileStorage;

        public SecondaryTileManager(DiscordClient client)
        {
            _discord = client;
            _tileStorage = new ConcurrentDictionary<ulong, SecondaryTile>();
        }

        public void AddAndUpdateTile(DiscordChannel channel, SecondaryTile tile)
        {
            _tileStorage[channel.Id] = tile;
            ClearTileNotifications(channel, tile);
        }

        public async Task InitialiseAsync()
        {
            var tiles = await SecondaryTile.FindAllAsync("App");
            foreach (var tile in tiles)
            {
                var id = tile.TileId;
                if (!id.Contains('_'))
                    continue;
                if (!ulong.TryParse(id.Substring(id.IndexOf('_')), out var ul))
                    continue;
                if (!_discord.TryGetCachedChannel(ul, out var channel))
                    continue;

                _tileStorage[ul] = tile;
            }
        }

        public Task HandleMessageAsync(DiscordClient client, DiscordMessage message)
        {
            if (_tileStorage.TryGetValue(message.Channel.Id, out var tile))
            {
                var updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
                var tileNotification = NotificationUtils.CreateTileNotificationForMessage(client, message);
                updater.EnableNotificationQueue(true);
                updater.Update(tileNotification);
            }

            return Task.CompletedTask;
        }

        public Task HandleAcknowledgeAsync(DiscordChannel channel)
        {
            if (_tileStorage.TryGetValue(channel.Id, out var tile))
            {
                ClearTileNotifications(channel, tile);
            }

            return Task.CompletedTask;
        }

        public static void ClearTileNotifications(DiscordChannel channel, SecondaryTile tile)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
            var tileNotification = NotificationUtils.CreateTileNotificationForChannel(channel);
            updater.EnableNotificationQueue(false);
            updater.Update(tileNotification);
        }
    }
}
