using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class VoiceStateGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordVoiceState state)
            {
                if (state.IsSelfDeafened || state.IsServerDeafened)
                {
                    return "\xE74F";
                }
                else if(state.IsServerMuted || state.IsSelfMuted)
                {
                    return "\xEC54";
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
