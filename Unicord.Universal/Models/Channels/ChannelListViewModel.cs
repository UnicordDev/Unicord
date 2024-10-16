using DSharpPlus.Entities;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelListViewModel : ChannelViewModel
    {
        private readonly GuildChannelListViewModel _guildChannelList;

        public ChannelListViewModel(DiscordChannel channel, GuildChannelListViewModel guildChannelList = null) 
            : base(channel.Id)
        {
            _guildChannelList = guildChannelList;
        }
    }
}
