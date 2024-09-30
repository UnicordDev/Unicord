using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Extensions;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    internal class BadgeManager
    {
        private BadgeUpdater _badgeUpdateManager;
        private DiscordClient _discord;

        public BadgeManager(DiscordClient client)
        {
            _discord = client;
            _badgeUpdateManager = BadgeUpdateManager.CreateBadgeUpdaterForApplication("App");
        }

        public void Update()
        {
            try
            {
                var mentions = 0;
                var unread = false;
                foreach (var (key, value) in _discord.ReadStates)
                {
                    if (_discord.TryGetCachedChannel(key, out var channel) && !channel.IsMuted() 
                        && (channel.Guild == null || !channel.Guild.IsMuted()))
                    {
                        unread |= channel.IsUnread();
                        mentions += value.MentionCount;
                    }
                }

                if (!unread)
                {
                    _badgeUpdateManager.Clear();
                    return;
                }

                var badgeXml = BadgeUpdateManager.GetTemplateContent(
                    mentions == 0 ? BadgeTemplateType.BadgeGlyph : BadgeTemplateType.BadgeNumber);
                var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

                if (mentions != 0)
                {
                    badgeElement.SetAttribute("value", mentions.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    badgeElement.SetAttribute("value", "available");
                }

                _badgeUpdateManager.Update(new BadgeNotification(badgeXml));
            }
            catch (Exception)
            {
                // TODO: log
            }
        }
    }
}
