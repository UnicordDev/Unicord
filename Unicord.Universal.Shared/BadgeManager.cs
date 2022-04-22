using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    public class BadgeManager
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
                var unread = _discord.Guilds.Values.Any(g => g.Unread);
                if (!unread)
                    return;

                //var badgeNumber = _discord.Guilds.Values.Sum(g => Math.Max(g.MentionCount, 0)) + _discord.PrivateChannels.Values.Sum(g => Math.Max(g.ReadState?.MentionCount ?? 0, 0));
                //if (badgeNumber != 0)
                //{
                //    var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
                //    var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

                //    badgeElement.SetAttribute("value", badgeNumber.ToString());
                //    _badgeUpdateManager.Update(new BadgeNotification(badgeXml));
                //}
                ////else if (unread)
                ////{
                ////    var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeGlyph);
                ////    var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

                ////    badgeElement.SetAttribute("value", "available");
                ////    _badgeUpdateManager.Update(new BadgeNotification(badgeXml));
                ////}
                //else
                //{
                //    _badgeUpdateManager.Clear();
                //}
            }
            catch (Exception)
            {
                // TODO: log
            }
        }
    }
}
