using System;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Unicord.Universal.Extensions
{
    internal static class MutedExtensions
    {
        public static bool IsMuted(this DiscordChannel channel)
        {
            if (!(channel.Discord is DiscordClient client))
                return false;

            if (!client.UserGuildSettings.TryGetValue(channel.GuildId ?? 0, out var settings))
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
            if (!(channel.Discord is DiscordClient discord))
                return false;

            var readState = channel.ReadState;

            // this shit should never happen but apparently it does sometimes, don't question it
            if (readState == null || readState.Id == 0)
                return false;

            if (discord == null)
                return false;

            if (channel.Type == ChannelType.Voice || channel.Type == ChannelType.Category)
                return false;

            if (channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
            {
                return readState.MentionCount > 0;
            }

            return (readState.MentionCount > 0 || (channel.LastMessageId != 0 && channel.LastMessageId > readState.LastMessageId));
        }
    }
}
