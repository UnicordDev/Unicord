using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Extensions;
using Humanizer;

namespace Unicord.Universal.Models.Channels
{
    public class DmChannelListViewModel : ChannelViewModel
    {
        private string _name;
        private string _avatarUrl;

        public DmChannelListViewModel(DiscordDmChannel channel) : base(channel, false, null)
        {
            _name = DmChannel.Name ?? DmChannel.Recipients.Select(s => s.DisplayName).Humanize();
            if (channel.Type == ChannelType.Private)
                AvatarUrl = channel.Recipients.First().GetAvatarUrl(32);
        }

        public override DiscordChannel Channel 
            => base.Channel as DiscordDmChannel;

        public override string Name 
            => _name;

        public DiscordDmChannel DmChannel 
            => (DiscordDmChannel)Channel;

        public string AvatarUrl { get => _avatarUrl; set => OnPropertySet(ref _avatarUrl, value); }
    }
}