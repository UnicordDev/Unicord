using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Unicord.Universal.Extensions
{
    public static class MutedExtensions
    {
        public static bool IsMuted(this DiscordChannel channel)
        {
            if (!(channel.Discord is DiscordClient client))
                return false;

            if (!client.UserGuildSettings.TryGetValue(channel.GuildId, out var settings))
                return false;

            var channelOverride = settings.ChannelOverrides?.FirstOrDefault(o => o?.ChannelId == channel.Id);
            if (channelOverride == null)
                return false;

            if (channelOverride.MuteConfig != null)
            {
                var endTime = channelOverride.MuteConfig.EndTime;
                if (endTime.HasValue && channelOverride.Muted)
                    return endTime.Value > DateTimeOffset.Now;
            }

            return channelOverride.Muted;
        }

        public static bool IsMuted(this DiscordGuild guild)
        {
            if (!(guild.Discord is DiscordClient client))
                return false;

            if (!client.UserGuildSettings.TryGetValue(guild.Id, out var settings) || settings == null)
                return false;

            if (settings.MuteConfig != null)
            {
                var endTime = settings.MuteConfig.EndTime;
                if (endTime.HasValue && settings.Muted)
                    return endTime.Value > DateTimeOffset.Now;
            }

            return settings.Muted;
        }

        public static bool IsUnread(this DiscordChannel channel)
        {
            var discord = (DiscordClient)channel.Discord;
            var readState = channel.ReadState;

            // this shit should never happen but apparently it does sometimes, don't question it
            if (readState.Id == 0)
                return false;

            if (discord == null || discord.IsDisposed)
                return false;

            if (channel.Type == ChannelType.Voice || channel.Type == ChannelType.Category || channel.Type == ChannelType.Store)
                return false;

            if (channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
            {
                return readState.MentionCount > 0;
            }

            return (readState.MentionCount > 0 || (channel.LastMessageId != 0 && channel.LastMessageId > readState.LastMessageId));
        }
    }
}
