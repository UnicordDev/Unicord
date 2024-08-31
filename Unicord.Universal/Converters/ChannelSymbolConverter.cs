using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Channels;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class ChannelSymbolConverter : IValueConverter
    {
        public string NSFWGlyph { get; set; } = "\xE7BA";
        public string TextGlyph { get; set; } = "\xE8BD";
        public string VoiceGlyph { get; set; } = "\xE767";
        public string NewsGlyph { get; set; } = "\xE789";
        public string StoreGlyph { get; set; } = "\xE719";
        public string StageGlyph { get; set; } = "\xE93E";
        public string ForumGlyph { get; set; } = "\xE93E";
        public string DirectoryGlyph { get; set; } = "\xE93E";
        public string UnknownGlyph { get; set; } = "\xE11B";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var type = ChannelType.Unknown;
            var isNSFW = false;

            switch (value)
            {
                case DiscordChannel c:
                    type = c.Type;
                    isNSFW = c.IsNSFW;
                    break;
                case ChannelListViewModel vm:
                    type = vm.ChannelType;
                    isNSFW = vm.Channel.IsNSFW;
                    break;
                case ChannelType t:
                    type = t;
                    break;
            }

            if (isNSFW)
            {
                return NSFWGlyph;
            }

            return type switch
            {
                ChannelType.Text => TextGlyph,
                ChannelType.Voice => VoiceGlyph,
                ChannelType.Announcement => NewsGlyph,
                //ChannelType.Store => StoreGlyph,
                ChannelType.Stage => StageGlyph,
                //ChannelType.GuildDirectory => DirectoryGlyph,
                ChannelType.GuildForum => ForumGlyph,
                _ => UnknownGlyph,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
