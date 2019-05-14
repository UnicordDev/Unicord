using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Pages.Management
{
    public class ChannelEditViewModel
    {
        private DiscordChannel _channel;

        public ChannelEditViewModel(DiscordChannel channel)
        {
            _channel = channel;

            Name = channel.Name;
            Topic = channel.Topic;
            NSFW = channel.IsNSFW;
        }

        public string Name { get; set; }
        
        public string Topic { get; set; }

        public bool NSFW { get; set; }

        public async Task SaveChangesAsync()
        {
            await _channel.ModifyAsync(m =>
            {
                m.Name = Name;
                m.Topic = Topic;
                m.Nsfw = NSFW;
            });
        }
    }
}
