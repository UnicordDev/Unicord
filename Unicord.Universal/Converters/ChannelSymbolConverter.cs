using System;
using DSharpPlus;
using DSharpPlus.Entities;
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
        public string UnknownGlyph { get; set; } = "\xE11B";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordChannel c)
            {
                if (c.IsNSFW)
                {
                    return NSFWGlyph;
                }

                var type = c.Type;
                switch (type)
                {
                    case ChannelType.Text:
                        return TextGlyph;
                    case ChannelType.Voice:
                        return VoiceGlyph;
                    case ChannelType.News:
                        return NewsGlyph;
                    case ChannelType.Store:
                        return StoreGlyph;
                    case ChannelType.Stage:
                        return StageGlyph;
                    default:
                        return "";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
