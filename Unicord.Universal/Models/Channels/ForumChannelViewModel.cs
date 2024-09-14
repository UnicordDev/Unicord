using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Models.User;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Models.Channels
{
    public class ForumThreadsCollection : ObservableCollection<ForumThreadViewModel>, ISupportIncrementalLoading
    {
        private readonly ForumChannelViewModel viewModel;
        private readonly DiscordClient client;
        private int fetched = 0;
        private int total = 1;

        public ForumThreadsCollection(
            ForumChannelViewModel viewModel,
            DiscordClient client)
        {
            this.viewModel = viewModel;
            this.client = client;
            this.HasMoreItems = true;
        }

        public bool HasMoreItems { get; private set; }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var dispatcher = Window.Current.Dispatcher;
            return Task.Run(async () =>
            {
                viewModel.IsLoading = true;
                var toFetch = (int)Math.Min(Math.Min(count, 25), total - fetched);
                if (toFetch <= 0)
                {
                    HasMoreItems = false;
                    viewModel.IsLoading = false;
                    return new LoadMoreItemsResult() { Count = 0 };
                }

                var forum = viewModel.ForumChannel;
                var search = await client.SearchForumChannelAsync(forum, true, false, toFetch, fetched, []);

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    foreach (var item in search.Threads)
                    {
                        var firstMessage = search.FirstMessages.FirstOrDefault(m => m.ChannelId == item.Id);
                        this.Add(new ForumThreadViewModel(item, firstMessage, viewModel));
                    }

                    HasMoreItems = search.HasMore;
                    total = search.TotalResults.Value;
                    fetched += search.Threads.Count;
                    viewModel.IsLoading = false;
                });

                return new LoadMoreItemsResult() { Count = (uint)search.Threads.Count };
            }).AsAsyncOperation();
        }
    }

    public class ForumThreadViewModel : ChannelViewModel
    {
        private MessageViewModel firstMessage;
        private UserViewModel creator;

        internal ForumThreadViewModel(DiscordThreadChannel channel, DiscordMessage firstMessage, ViewModelBase parent = null)
            : base(channel, false, parent)
        {
            if (Channel is not DiscordThreadChannel thread)
                throw new InvalidOperationException();

            if (thread.FirstMessage != null)
                this.firstMessage = new MessageViewModel(thread.FirstMessage);
            else if (firstMessage != null)
                this.firstMessage = new MessageViewModel(firstMessage);


            creator = new UserViewModel(thread.CreatorId, thread.GuildId, this);
        }

        public DiscordThreadChannel Thread
            => (DiscordThreadChannel)Channel;

        public MessageViewModel FirstMessage
        {
            get => firstMessage;
            internal set
            {
                OnPropertySet(ref firstMessage, value);
                InvokePropertyChanged(nameof(ShowAttachment));
                InvokePropertyChanged(nameof(DisplayAttachment));
            }
        }

        public UserViewModel Creator
        {
            get => creator;
            internal set
            {
                OnPropertySet(ref creator, value);
                InvokePropertyChanged(nameof(HasValidCreator));
            }
        }

        public bool HasValidCreator
            => !string.IsNullOrWhiteSpace(Creator.DisplayName);

        public bool ShowAttachment
            => DisplayAttachment != null;

        public AttachmentViewModel DisplayAttachment
            => FirstMessage?.Attachments.FirstOrDefault(a => !double.IsNaN(a.NaturalWidth));
    }

    public class ForumChannelViewModel : ChannelViewModel
    {
        internal ForumChannelViewModel(ulong channelId, ViewModelBase parent = null)
            : base(channelId, false, parent)
        {
            if (Channel is not DiscordForumChannel forum)
                throw new InvalidOperationException();

            IsLoading = true;
            ForumThreads = new ForumThreadsCollection(this, discord);

            foreach (var thread in ForumChannel.Threads.OrderByDescending(t => t.FirstMessage?.CreationTimestamp))
                ForumThreads.Add(new ForumThreadViewModel(thread, null, this));
        }

        public DiscordForumChannel ForumChannel
            => (DiscordForumChannel)Channel;

        public ForumThreadsCollection ForumThreads { get; }

        public async Task LoadPostData()
        {
            if (!ForumChannel.CurrentPermissions.HasPermission(Permissions.ReadMessageHistory) || !ForumChannel.Threads.Any())
                return;

            await ForumChannel.Guild.RequestUserPresencesAsync(ForumChannel.Threads.Select(c => c.CreatorId));

            var result = await discord.GetForumPostDataAsync(ForumChannel, ForumChannel.Threads.Select(t => t.Id).ToArray())
                .ConfigureAwait(false);

            syncContext.Post((o) =>
            {
                foreach (var thread in result.ThreadData)
                {
                    var viewModel = ForumThreads.FirstOrDefault(v => v.Id == thread.Key);
                    if (viewModel == null) continue;

                    var data = thread.Value;
                    if (data.FirstMessage != null)
                        viewModel.FirstMessage = new MessageViewModel(data.FirstMessage);
                    if (data.Creator != null)
                        viewModel.Creator = new UserViewModel(data.Creator, ForumChannel.GuildId);
                }
            }, null);
        }
    }
}
