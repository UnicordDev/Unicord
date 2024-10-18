using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Channels
{
    public class ForumChannelViewModel : ChannelPageViewModelBase
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
