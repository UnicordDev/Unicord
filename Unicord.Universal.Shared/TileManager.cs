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
using Unicord.Universal.Extensions;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    public class TileManager
    {
        private readonly DiscordClient _discord = null;
        private readonly TileUpdater _tileUpdater = null;
        private readonly List<DiscordMessage> _currentUnreads = null;
        private readonly SemaphoreSlim _semaphore;

        public TileManager(DiscordClient client)
        {
            _discord = client;
            _tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication("App");
            _currentUnreads = new List<DiscordMessage>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task InitialiseAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var unreads = await FetchUnreadMessagesAsync();
                _currentUnreads.AddRange(unreads);

                Update();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task HandleMessageAsync(DiscordMessage message)
        {
            await _semaphore.WaitAsync();
            try
            {
                _currentUnreads.RemoveAll(m => m.Channel == message.Channel);
                _currentUnreads.Insert(0, message);

                Update();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task HandleAcknowledgeAsync(DiscordChannel channel)
        {
            await _semaphore.WaitAsync();
            try
            {
                _currentUnreads.RemoveAll(m => m.Channel == channel);

                Update();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Update()
        {
            _tileUpdater.EnableNotificationQueue(true);
            _tileUpdater.Clear();

            foreach (var message in _currentUnreads.Take(5))
            {
                var tileContent = NotificationUtils.CreateTileNotificationForMessage(_discord, message);
                _tileUpdater.Update(tileContent);
            }
        }

        private async Task<IReadOnlyList<DiscordMessage>> FetchUnreadMessagesAsync()
        {
            var unreadChannelTasks = _discord.PrivateChannels.Values
                .Where(c => c.ReadState != null && c.IsUnread() && c.ReadState.LastMessageId != 0)
                .Take(5)
                .Select(c => c.GetMessagesAroundAsync(c.ReadState.LastMessageId, 10)
                              .ContinueWith(t => (messageId: c.ReadState.LastMessageId, messages: t.Result), TaskContinuationOptions.OnlyOnRanToCompletion));
            var channelMessages = await Task.WhenAll(unreadChannelTasks);
            return channelMessages.Select(x => x.messages.FirstOrDefault(m => m.Id > x.messageId)).Where(m => m != null)
                            .Concat(await _discord.GetMentionsAsync(5))
                            .OrderByDescending(m => m.Id)
                            .ToList();
        }
    }
}
