using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Models.Messages;

namespace Unicord.Universal.Models.Channels
{
    internal class PinsPageViewModel : ViewModelBase
    {
        private bool isLoading;
        private bool isErrored;
        private bool isRateLimited;

        private readonly DiscordChannel _channel;
        private readonly ILogger<PinsPageViewModel> _logger
            = Logger.GetLogger<PinsPageViewModel>();

        public PinsPageViewModel()
        {

        }

        public PinsPageViewModel(ChannelViewModel channel)
            : base(channel)
        {
            this._channel = channel.Channel;

            Messages.CollectionChanged += (o, e) =>
            {
                if (!IsLoading)
                    UnsafeInvokePropertyChanged(nameof(NoMessages));
            };

            WeakReferenceMessenger.Default.Register<PinsPageViewModel, MessageUpdateEventArgs>(this, static (r, m) => r.OnMessageUpdated(m.Event));

            Task.Run(LoadAsync);
        }

        public ObservableCollection<MessageViewModel> Messages { get; } = [];

        public bool NoMessages
            => !IsErrored && !IsLoading && !IsRateLimited && Messages.Count == 0;

        public bool IsLoading
        {
            get => isLoading;
            set => OnPropertySet(ref isLoading, value, nameof(IsLoading), nameof(NoMessages));
        }

        public bool IsRateLimited
        {
            get => isRateLimited;
            set => OnPropertySet(ref isRateLimited, value, nameof(IsRateLimited), nameof(NoMessages));
        }

        public bool IsErrored
        {
            get => isErrored;
            set => OnPropertySet(ref isErrored, value, nameof(IsErrored), nameof(NoMessages));
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            IsRateLimited = false;
            IsErrored = false;
            try
            {
                var pinnedMessages = await _channel.GetPinnedMessagesAsync();
                syncContext.Post((o) =>
                {
                    foreach (var message in pinnedMessages)
                    {
                        Messages.Add(new MessageViewModel(message));
                    }
                }, null);
            }
            catch (RateLimitException ex)
            {
                _logger.LogWarning(ex, "Rate limited when loading pinned messages for channel {Channel}", _channel);
                IsRateLimited = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pinned messages for channel {Channel}", _channel);
                IsErrored = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnMessageUpdated(MessageUpdateEventArgs ev)
        {
            if (ev.Channel.Id != _channel.Id)
                return;

            syncContext.Post((o) =>
            {
                var vm = Messages.FirstOrDefault(m => m.Id == ev.Message.Id);

                if (ev.Message.Pinned)
                {
                    if (vm == null)
                        Messages.Add(new MessageViewModel(ev.Message));
                }
                else
                {
                    if (vm != null)
                        Messages.Remove(vm);
                }
            }, null);
        }
    }
}
