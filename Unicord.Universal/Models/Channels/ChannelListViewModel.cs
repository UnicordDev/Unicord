using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
