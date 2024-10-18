using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
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
}
