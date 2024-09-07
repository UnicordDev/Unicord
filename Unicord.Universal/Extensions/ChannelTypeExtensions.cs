using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Unicord.Universal.Extensions
{
    internal static class ChannelExtensions
    {
        public static bool IsText(this DiscordChannel channel) =>
            channel.Type is (ChannelType.Text or ChannelType.Announcement or ChannelType.Private or ChannelType.Group or ChannelType.PrivateThread or ChannelType.PublicThread or ChannelType.AnnouncementThread);
        public static bool IsVoice(this DiscordChannel channel) =>
            channel.Type == ChannelType.Voice || channel.Type == ChannelType.Stage;

    }
}
