using DSharpPlus.Entities;
using Windows.UI;

namespace Unicord.Universal.Extensions
{
    public static class DiscordColorExtensions
    {
        public static Color ToColor(this DiscordColor color)
        {
            // Color.FromArgb is a native call lmao
            return new Color() { A = 255, R = color.R, G = color.G, B = color.B };
        }
    }
}
