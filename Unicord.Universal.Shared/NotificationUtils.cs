using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using MomentSharp;
using WamWooWam.Core;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    public class NotificationUtils
    {
        public static bool WillShowToast(DiscordMessage message)
        {
            bool willNotify = false;

            if (message.MentionedUsers.Any(m => m?.Id == message.Discord.CurrentUser.Id))
            {
                willNotify = true;
            }

            if (message.Channel is DiscordDmChannel)
            {
                willNotify = true;
            }

            if (message.Channel.Guild != null)
            {
                var usr = message.Channel.Guild.CurrentMember;
                if (message.MentionedRoleIds.Any(r => (usr.RoleIds.Contains(r))) == true)
                {
                    willNotify = true;
                }
            }

            if (message.Author.Id == message.Discord.CurrentUser.Id)
            {
                willNotify = false;
            }

            if ((message.Discord as DiscordClient)?.UserSettings.Status == "dnd")
            {
                willNotify = false;
            }

            if (message.Channel.NotificationMuted)
                willNotify = false;

            return willNotify;
        }


        public static string GetMessageTitle(DiscordMessage message) => message.Channel.Guild != null ?
                             $"{(message.Author as DiscordMember)?.DisplayName ?? message.Author.Username} in {message.Channel.Guild.Name}" :
                             $"{message.Author.Username}";

        public static string GetMessageContent(DiscordMessage message)
        {
            string messageText = message.Content;

            foreach (var user in message.MentionedUsers)
            {
                if (user != null)
                {
                    messageText = messageText
                        .Replace($"<@{user.Id}>", $"@{user.Username}")
                        .Replace($"<@!{user.Id}>", $"@{user.Username}");
                }
            }

            if (message.Channel.Guild != null)
            {
                foreach (var channel in message.MentionedChannels)
                {
                    messageText = messageText.Replace(channel.Mention, $"#{channel.Name}");
                }

                foreach (var roleId in message.MentionedRoleIds)
                {
                    var role = message.Channel.Guild.GetRole(roleId);
                    if (role != null)
                        messageText = messageText.Replace(role.Mention, $"@{role.Name}");
                }
            }

            return messageText;
        }

        public static string GetChannelHeaderName(DiscordChannel channel)
        {
            if (channel is DiscordDmChannel dmChannel)
            {
                if (dmChannel.Type == ChannelType.Private)
                {
                    return $"@{dmChannel.Recipients[0].DisplayName}";
                }

                if (dmChannel.Type == ChannelType.Group)
                {
                    return dmChannel.Name ?? Strings.NaturalJoin(dmChannel.Recipients.Select(r => r.DisplayName));
                }
            }

            if (channel.Guild != null)
                return $"#{channel.Name} - {channel.Guild.Name}";

            return $"#{channel.Name}";
        }

        public static TileNotification CreateTileNotificationForMessage(DiscordMessage message)
        {
            var tileContentBuilder = new TileContentBuilder()
                .SetBranding(TileBranding.NameAndLogo)
                .AddTile(TileSize.Large)
                .AddTile(TileSize.Medium)
                .AddTile(TileSize.Wide);

            if (message.Channel is DiscordDmChannel dm)
            {
                if (!string.IsNullOrEmpty(dm.IconHash))
                    tileContentBuilder.SetPeekImage(new Uri(dm.IconUrl));

                if (!string.IsNullOrEmpty(dm.Recipient?.AvatarHash))
                    tileContentBuilder.SetPeekImage(new Uri(dm.Recipient.GetAvatarUrl(ImageFormat.Png)));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetChannelHeaderName(message.Channel), HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetMessageContent(message), HintStyle = AdaptiveTextStyle.Caption, HintWrap = true })
                    .SetDisplayName(new Moment(message.Timestamp.UtcDateTime).Calendar());
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(message.Channel.Guild.IconUrl))
                    tileContentBuilder.SetPeekImage(new Uri(message.Channel.Guild.IconUrl + "?size=1024"));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = $"#{message.Channel.Name}", HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = message.Channel.Guild.Name, HintStyle = AdaptiveTextStyle.Body })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetMessageContent(message), HintStyle = AdaptiveTextStyle.Caption, HintWrap = true })
                    .SetDisplayName(new Moment(message.Timestamp.UtcDateTime).Calendar());
            }

            var tileContent = tileContentBuilder.GetTileContent();
            var doc = new XmlDocument();
            doc.LoadXml(tileContent.GetContent());

            return new TileNotification(doc);
        }

        public static TileNotification CreateTileNotificationForChannel(DiscordChannel channel)
        {
            var tileContentBuilder = new TileContentBuilder()
                .SetBranding(TileBranding.NameAndLogo)
                .AddTile(TileSize.Large)
                .AddTile(TileSize.Medium)
                .AddTile(TileSize.Wide);

            if (channel is DiscordDmChannel dm)
            {
                if (!string.IsNullOrEmpty(dm.IconHash))
                    tileContentBuilder.SetPeekImage(new Uri(dm.IconUrl));

                if (!string.IsNullOrEmpty(dm.Recipient?.AvatarHash))
                    tileContentBuilder.SetPeekImage(new Uri(dm.Recipient.AvatarUrl));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetChannelHeaderName(channel), HintStyle = AdaptiveTextStyle.Base });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(channel.Guild.IconUrl))
                    tileContentBuilder.SetPeekImage(new Uri(channel.Guild.IconUrl + "?size=1024"));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = $"#{channel.Name}", HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = channel.Guild.Name, HintStyle = AdaptiveTextStyle.Body });
            }

            var tileContent = tileContentBuilder.GetTileContent();
            var doc = new XmlDocument();
            doc.LoadXml(tileContent.GetContent());

            return new TileNotification(doc);
        }

        public static ToastNotification CreateToastNotificationForMessage(DiscordMessage message)
        {
            var title = GetMessageTitle(message);
            var text = GetMessageContent(message);
            var channelName = GetChannelHeaderName(message.Channel);

            var animated = message.Author.AvatarHash?.StartsWith("a_") ?? false;
            var avatarUrl = message.Author.GetAvatarUrl(animated ? ImageFormat.Gif : ImageFormat.Png, 128);
            var replyString = message.Channel is DiscordDmChannel ? $"Reply to @{message.Author.Username}..." : $"Message #{message.Channel.Name}...";

            var builder = new ToastContentBuilder()
                .AddHeader(message.ChannelId.ToString(), channelName, $"-channelId={message.ChannelId}")
                .AddAppLogoOverride(new Uri(avatarUrl), ToastGenericAppLogoCrop.Circle)
                .AddText(title, AdaptiveTextStyle.Title)
                .AddText(text, AdaptiveTextStyle.Caption)
                .AddInputTextBox("tbReply", replyString)
                .AddButton("tbReply", "Reply", ToastActivationType.Background, $"-channelId={message.ChannelId}")
                .AddToastActivationInfo($"-channelId={message.ChannelId} -messageId={message.Id}", ToastActivationType.Foreground)
                .AddCustomTimeStamp(message.Timestamp.DateTime);

            if (GetToastThumbnail(message, out var width, out var height, out var proxyUrl))
            {
                Drawing.ScaleProportions(ref width, ref height, 640, 360);
                builder.AddHeroImage(new Uri(proxyUrl + $"?format=jpeg&width={(int)width}&height={(int)height}"));
            }

            var toastContent = builder.GetToastContent();
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());
            return new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Allowed,
                Group = message.Channel.Id.ToString(),
                RemoteId = message.Id.ToString(),
            };
        }

        private static bool GetToastThumbnail(DiscordMessage message, out double width, out double height, out string proxyUrl)
        {
            width = 0;
            height = 0;
            proxyUrl = null;

            var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
            var embed = message.Embeds.FirstOrDefault(e => e.Thumbnail != null || e.Image != null || e.Video != null);
            if (attach != null)
            {
                width = attach.Width;
                height = attach.Height;
                proxyUrl = attach.ProxyUrl;
                return true;
            }
            else if (embed != null)
            {
                width = embed.Thumbnail?.Width ?? embed.Image?.Width ?? embed.Video.Width;
                height = embed.Thumbnail?.Height ?? embed.Image?.Height ?? embed.Video.Height;
                proxyUrl = (embed.Thumbnail?.ProxyUrl ?? embed.Image?.ProxyUrl ?? embed.Video.Url).ToString();
                return true;
            }

            return false;
        }
    }
}
