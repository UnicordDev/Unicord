using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    // todo: both of these should be in a viewmodel
    public class SystemMessageSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MessageViewModel message)
            {
                return message.Message.MessageType switch
                {
                    MessageType.RecipientAdd => "\xE72A",
                    MessageType.RecipientRemove => "\xE72B",
                    MessageType.Call => "\xE717",
                    MessageType.ChannelNameChange
                        or MessageType.ChannelIconChange => "\xE70F",
                    MessageType.ChannelPinnedMessage => "\xE840",
                    MessageType.GuildMemberJoin => "\xE72A",
                    MessageType.UserPremiumGuildSubscription
                        or MessageType.TierOneUserPremiumGuildSubscription
                        or MessageType.TierTwoUserPremiumGuildSubscription
                        or MessageType.TierThreeUserPremiumGuildSubscription => "\xECAD",
                    _ => "\xE783",
                };
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class SystemMessageTextConverter : IValueConverter
    {
        private ResourceLoader _strings;

        static SystemMessageTextConverter()
        {
            WelcomeStrings = ImmutableArray.Create(
                "{0} just joined the server - glhf!",
                "{0} just joined. Everyone, look busy!",
                "{0} just joined. Can I get a heal?",
                "{0} joined your party.",
                "{0} joined. You must construct additional pylons.",
                "Ermagherd. {0} is here.",
                "Welcome, {0}. Stay awhile and listen.",
                "Welcome, {0}. We were expecting you ( \u0361\u00B0 \u035C\u0296 \u0361\u00B0)", // ( ͡° ͜ʖ ͡°)
                "Welcome, {0}. We hope you brought pizza.",
                "Welcome {0}. Leave your weapons by the door.",
                "A wild {0} appeared.",
                "Swoooosh. {0} just landed.",
                "Brace yourselves. {0} just joined the server.",
                "{0} just joined... or did they?",
                "{0} just arrived. Seems OP - please nerf.",
                "{0} just slid into the server.",
                "A {0} has spawned in the server.",
                "Big {0} showed up!",
                "Where’s {0}? In the server!",
                "{0} hopped into the server. Kangaroo!!",
                "{0} just showed up. Hold my beer.",
                "Challenger approaching - {0} has appeared!",
                "It's a bird! It's a plane! Nevermind, it's just {0}.",
                "It's {0}! Praise the sun! \\\\[T]/",
                "Never gonna give {0} up. Never gonna let {0} down.",
                "{0} has joined the battle bus.",
                "Cheers, love! {0}'s here!",
                "Hey! Listen! {0} has joined!",
                "We've been expecting you {0}",
                "It's dangerous to go alone, take {0}!",
                "{0} has joined the server! It's super effective!",
                "Cheers, love! {0} is here!",
                "{0} is here, as the prophecy foretold.",
                "{0} has arrived. Party's over.",
                "Ready player {0}",
                "{0} is here to kick gum and chew ass. And {0} is all out of ass.",
                "Hello. Is it {0} you're looking for?",
                "{0} has joined. Stay a while and listen!",
                "Roses are red, violets are blue, {0} joined this server with you");
        }

        public static ImmutableArray<string> WelcomeStrings { get; }

        public SystemMessageTextConverter()
        {
            _strings = ResourceLoader.GetForViewIndependentUse("Converters");
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MessageViewModel message)
            {
                switch (message.Message.MessageType)
                {
                    case MessageType.RecipientAdd:
                        return string.Format(_strings.GetString("UserJoinedGroupFormat"), message.Author.Mention);
                    case MessageType.RecipientRemove:
                        return string.Format(_strings.GetString("UserLeftGroupFormat"), message.Author.Mention);
                    case MessageType.Call:
                        return string.Format(_strings.GetString("UserStartedCallFormat"), message.Author.Mention);
                    case MessageType.ChannelNameChange:
                        return string.Format(_strings.GetString("UserChannelNameChangeFormat"), message.Author.Mention);
                    case MessageType.ChannelIconChange:
                        return string.Format(_strings.GetString("UserChannelIconChangeFormat"), message.Author.Mention);
                    case MessageType.ChannelPinnedMessage:
                        return string.Format(_strings.GetString("UserMessagePinFormat"), message.Author.Mention);
                    case MessageType.GuildMemberJoin:
                        return string.Format(WelcomeStrings[(int)(message.Message.CreationTimestamp.ToUnixTimeMilliseconds() % WelcomeStrings.Count())], message.Author.Mention);
                    case MessageType.UserPremiumGuildSubscription:
                        return string.Format(_strings.GetString(
                            string.IsNullOrWhiteSpace(message.Content) ?
                            "UserPremiumGuildSubscriptionFormat" :
                            "UserPremiumMultiGuildSubscriptionFormat"), message.Author.Mention, message.Content);
                    case MessageType.TierOneUserPremiumGuildSubscription:
                        return string.Format(_strings.GetString(
                            string.IsNullOrWhiteSpace(message.Content) ?
                            "UserPremiumGuildSubscriptionTierFormat" :
                            "UserPremiumMultiGuildSubscriptionTierFormat"), message.Author.Mention, message.Content, 1);
                    case MessageType.TierTwoUserPremiumGuildSubscription:
                        return string.Format(_strings.GetString(
                            string.IsNullOrWhiteSpace(message.Content) ?
                            "UserPremiumGuildSubscriptionTierFormat" :
                            "UserPremiumMultiGuildSubscriptionTierFormat"), message.Author.Mention, message.Content, 2);
                    case MessageType.TierThreeUserPremiumGuildSubscription:
                        return string.Format(_strings.GetString(
                            string.IsNullOrWhiteSpace(message.Content) ?
                            "UserPremiumGuildSubscriptionTierFormat" :
                            "UserPremiumMultiGuildSubscriptionTierFormat"), message.Author.Mention, message.Content, 3);
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
