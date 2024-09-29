using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using MomentSharp;
using Unicord.Universal.Extensions;
using WamWooWam.Core;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    internal class NotificationUtils
    {
        public static bool WillShowToast(BaseDiscordClient client, DiscordMessage message)
        {
            if (message.Author.IsCurrent)
                return false;

            if (client is DiscordClient discord && discord.UserSettings?.Status == "dnd")
                return false;

            if (message.Channel.IsMuted() || (message.Channel.Guild != null && message.Channel.Guild.IsMuted()) || !message.Channel.IsUnread())
                return false;

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

            return willNotify;
        }

        private static readonly Regex UserRegex = new Regex(@"<@!?(\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex RoleRegex = new Regex(@"<@&(\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex ChannelRegex = new Regex(@"<#(\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex EmojiRegex = new Regex(@"<a?:([a-zA-Z0-9_]+):(\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex TimestampRegex = new Regex(@"<t:(-?\d{1,13})(:([DFRTdft]))?>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        private static readonly Regex SpoilerRegex = new Regex("\\|\\|((?:.|\\r|\\n)*?)\\|\\|", RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.Compiled);

        public static string GetMessageTitle(DiscordMessage message) => message.Channel.Guild != null ?
                             $"{message.Author.DisplayName} in {message.Channel.Guild.Name}" :
                             $"{message.Author.DisplayName}";

        public static string GetMessageContent(BaseDiscordClient client, DiscordMessage message)
        {
            string messageText = message.Content;

            messageText = SpoilerRegex.Replace(messageText, (match) => new string('\x2B1B', match.Value.Length - 4));

            messageText = UserRegex.Replace(messageText, (match) =>
            {
                if (ulong.TryParse(match.Groups[1].Value, out var id))
                {
                    if (message.Channel.Guild?.Members.TryGetValue(id, out var member) == true)
                        return $"@{member.DisplayName}";

                    if (client is DiscordClient discord && discord.TryGetCachedUser(id, out var user))
                        return $"@{user.DisplayName}";

                    return "@unknown-user";
                }

                return match.Value;
            });

            messageText = ChannelRegex.Replace(messageText, (match) =>
            {
                if (ulong.TryParse(match.Groups[1].Value, out var id))
                {
                    if (message.Channel.Guild?.Channels.TryGetValue(id, out var channel) == true)
                        return $"#{channel.Name}";

                    if (client is DiscordClient discord && discord.TryGetCachedChannel(id, out channel))
                        return $"#{channel.Name}";

                    return "#unknown-channel";
                }

                return match.Value;
            });

            messageText = EmojiRegex.Replace(messageText, (match) =>
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    return $":{match.Groups[1].Value}:";

                return match.Value;
            });

            if (message.Channel.Guild != null)
            {
                messageText = RoleRegex.Replace(messageText, (match) =>
                {
                    if (ulong.TryParse(match.Groups[1].Value, out var id))
                    {
                        if (message.Channel.Guild.Roles.TryGetValue(id, out var role))
                            return $"@{role.Name}";

                        return "@unknown-role";
                    }

                    return match.Value;
                });
            }
            if (messageText.Length > 128)
                messageText = messageText.Substring(0, 125) + "...";

            return messageText;
        }

        public static string GetChannelHeaderName(DiscordChannel channel)
        {
            if (channel is DiscordDmChannel dmChannel)
            {
                if (dmChannel.Type == ChannelType.Private)
                {
                    if (dmChannel.Recipients.Count == 0)
                        return "Invalid DM channel";

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

        public static TileNotification CreateTileNotificationForMessage(BaseDiscordClient client, DiscordMessage message)
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

                tileContentBuilder.SetPeekImage(new Uri(message.Author.GetAvatarUrl(ImageFormat.Png)));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetChannelHeaderName(message.Channel), HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetMessageContent(client, message), HintStyle = AdaptiveTextStyle.Caption, HintWrap = true })
                    .SetDisplayName(new Moment(message.Timestamp.UtcDateTime).Calendar());
            }
            else if (message.Channel is not null)
            {
                if (!string.IsNullOrWhiteSpace(message.Channel?.Guild.IconUrl))
                    tileContentBuilder.SetPeekImage(new Uri(message.Channel.Guild.IconUrl + "?size=512"));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = $"#{message.Channel.Name}", HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = message.Channel.Guild.Name, HintStyle = AdaptiveTextStyle.Body })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = GetMessageContent(client, message), HintStyle = AdaptiveTextStyle.Caption, HintWrap = true })
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

                if (dm.Type == ChannelType.Private && dm.Recipients.Count > 0)
                    tileContentBuilder.SetPeekImage(new Uri(dm.Recipients[0].AvatarUrl));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText()
                {
                    Text = GetChannelHeaderName(channel),
                    HintStyle = AdaptiveTextStyle.Base
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(channel.Guild.IconUrl))
                    tileContentBuilder.SetPeekImage(new Uri(channel.Guild.IconUrl + "?size=512"));

                tileContentBuilder.AddAdaptiveTileVisualChild(new AdaptiveText() { Text = $"#{channel.Name}", HintStyle = AdaptiveTextStyle.Base })
                    .AddAdaptiveTileVisualChild(new AdaptiveText() { Text = channel.Guild.Name, HintStyle = AdaptiveTextStyle.Body });
            }

            var tileContent = tileContentBuilder.GetTileContent();
            var doc = new XmlDocument();
            doc.LoadXml(tileContent.GetContent());

            return new TileNotification(doc);
        }

        public static ToastNotification CreateToastNotificationForMessage(BaseDiscordClient client, DiscordMessage message, bool isSuppressed = false)
        {
            var title = GetMessageTitle(message);
            var text = GetMessageContent(client, message);
            var channelName = GetChannelHeaderName(message.Channel);

            var avatarUrl = message.Author.GetAvatarUrl(ImageFormat.Png, 128);
            var replyString = message.Channel is DiscordDmChannel ? $"Reply to @{message.Author.DisplayName}..." : $"Message #{message.Channel.Name}...";

            var builder = new ToastContentBuilder()
                .AddHeader(message.ChannelId.ToString(), channelName, $"-channelId={message.ChannelId}")
                .AddAppLogoOverride(new Uri(avatarUrl), ToastGenericAppLogoCrop.Circle)
                .AddText(title, AdaptiveTextStyle.Title)
                .AddText(text, AdaptiveTextStyle.Caption)
                .AddInputTextBox("tbReply", replyString)
                .AddButton("tbReply", "Reply", ToastActivationType.Background, $"-channelId={message.ChannelId}")
                .AddToastActivationInfo($"-channelId={message.ChannelId} -messageId={message.Id}", ToastActivationType.Foreground)
                .AddAttributionText("from Discord")
                .AddCustomTimeStamp(message.Timestamp.DateTime)
                .AddAudio(new Uri("ms-winsoundevent:Notification.IM"));

            if (GetToastThumbnail(message, out var width, out var height, out var proxyUrl) &&
                !Path.GetFileName(proxyUrl).StartsWith("SPOILER_", StringComparison.InvariantCultureIgnoreCase))
            {
                Drawing.ScaleProportions(ref width, ref height, 728, 360);

                var uri = new UriBuilder(proxyUrl);
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["format"] = "jpeg";
                query["width"] = ((int)width).ToString(CultureInfo.InvariantCulture);
                query["height"] = ((int)height).ToString(CultureInfo.InvariantCulture);
                uri.Query = query.ToString();

                builder.AddHeroImage(uri.Uri);
            }

            var toastContent = builder.GetToastContent();

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            return new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Allowed,
                Tag = message.Id.ToString(),
                Group = message.Channel.Id.ToString(CultureInfo.InvariantCulture),
                RemoteId = message.Id.ToString(CultureInfo.InvariantCulture),
                SuppressPopup = isSuppressed
            };
        }

        private static bool GetToastThumbnail(DiscordMessage message, out double width, out double height, out string proxyUrl)
        {
            width = 0;
            height = 0;
            proxyUrl = null;

            var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
            var embed = message.Embeds.FirstOrDefault(e => e.Thumbnail != null || e.Image != null || e.Video != null);
            if (attach != null && attach.Width != null)
            {
                width = attach.Width.Value;
                height = attach.Height.Value;
                proxyUrl = attach.ProxyUrl;
                return true;
            }
            else if (embed != null)
            {
                width = embed.Thumbnail?.Width ?? embed.Image?.Width ?? embed.Video.Width;
                height = embed.Thumbnail?.Height ?? embed.Image?.Height ?? embed.Video.Height;
                proxyUrl = (embed.Thumbnail?.ProxyUrl.ToUri() ?? embed.Image?.ProxyUrl.ToUri() ?? embed.Video.Url).ToString();
                return true;
            }

            return false;
        }
    }
}
