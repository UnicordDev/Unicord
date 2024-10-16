using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Unicord.Universal.Extensions;

namespace Unicord.Universal.Models.Channels
{
    public class DmChannelListViewModel : ChannelViewModel
    {
        private string _name;
        private string _avatarUrl;

        public DmChannelListViewModel(DiscordDmChannel channel) 
            : base(channel, false, null)
        {
            _name = DmChannel.Name ?? DmChannel.Recipients.Select(s => s.DisplayName).Humanize();
            if (channel.Type == ChannelType.Private)
                AvatarUrl = channel.Recipients.FirstOrDefault()?.GetAvatarUrl(32);
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