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
            channel.Type == ChannelType.Text || channel.Type == ChannelType.Announcement || channel.Type == ChannelType.Private || channel.Type == ChannelType.Group;
        public static bool IsVoice(this DiscordChannel channel) =>
            channel.Type == ChannelType.Voice || channel.Type == ChannelType.Stage;

    }
}
