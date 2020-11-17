using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background
{
    public class BadgeManager
    {
        private BadgeUpdater _badgeUpdateManager;
        private DiscordClient _discord;
        private int? _currentBadge = null;

        public BadgeManager(DiscordClient client)
        {
            _discord = client;
            _badgeUpdateManager = BadgeUpdateManager.CreateBadgeUpdaterForApplication("App");
        }

        public void Update()
        {
            try
            {
                var unread = _discord.Guilds.Values.Any(g => g.Unread);
                if (!unread)
                    return;

                var badgeNumber = _discord.Guilds.Values.Sum(g => g.MentionCount) + _discord.PrivateChannels.Values.Sum(g => g.ReadState?.MentionCount);
                if (badgeNumber != 0)
                {
                    var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
                    var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

                    badgeElement.SetAttribute("value", badgeNumber.ToString());
                    _badgeUpdateManager.Update(new BadgeNotification(badgeXml));
                    _currentBadge = badgeNumber;
                }
                else if (unread)
                {
                    var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeGlyph);
                    var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

                    badgeElement.SetAttribute("value", "available");
                    _badgeUpdateManager.Update(new BadgeNotification(badgeXml));
                    _currentBadge = 0;
                }
                else
                {
                    _currentBadge = null;
                    _badgeUpdateManager.Clear();
                }
            }
            catch (Exception)
            {
                // TODO: log
            }
        }
    }
}
