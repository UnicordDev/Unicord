using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.Graphics.Display;

namespace Unicord.Universal.Extensions
{
    public static class GuildExtensions
    {
        public static string GetIconUrl(this DiscordGuild guild, int size)
        {
            if (size < 16 || size > 2048)
                throw new ArgumentOutOfRangeException(nameof(size));

            var displayInfo = DisplayInformation.GetForCurrentView();
            size = (int)BitOperations.RoundUpToPowerOf2((uint)(size * (displayInfo.LogicalDpi / 96.0f)));

            var fmt = Tools.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
            var sfmt = fmt switch
            {
                ImageFormat.Gif => "gif",
                ImageFormat.Jpeg => "jpg",
                ImageFormat.Png => "png",
                ImageFormat.WebP => "webp",
                _ => throw new ArgumentOutOfRangeException(nameof(fmt)),
            };

            var ssize = size.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(guild.IconHash))
            {
                var id = guild.Id.ToString(CultureInfo.InvariantCulture);
                return $"https://cdn.discordapp.com/icons/{id}/{guild.IconHash}.{sfmt}?size={ssize}";
            }
            else
            {
                return null;
            }
        }

        public static string GetBannerUrl(this DiscordGuild guild)
        {
            var fmt = Tools.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
            var sfmt = fmt switch
            {
                ImageFormat.Gif => "gif",
                ImageFormat.Jpeg => "jpg",
                ImageFormat.Png => "png",
                ImageFormat.WebP => "webp",
                _ => throw new ArgumentOutOfRangeException(nameof(fmt)),
            };

            if (!string.IsNullOrWhiteSpace(guild.Banner))
            {
                var id = guild.Id.ToString(CultureInfo.InvariantCulture);
                return $"https://cdn.discordapp.com/banners/{id}/{guild.Banner}.{sfmt}?size=512";
            }
            else
            {
                return null;
            }
        }

        public static string GetSplashUrl(this DiscordGuild guild)
        {
            var fmt = Tools.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
            var sfmt = fmt switch
            {
                ImageFormat.Gif => "gif",
                ImageFormat.Jpeg => "jpg",
                ImageFormat.Png => "png",
                ImageFormat.WebP => "webp",
                _ => throw new ArgumentOutOfRangeException(nameof(fmt)),
            };

            if (!string.IsNullOrWhiteSpace(guild.SplashHash))
            {
                var id = guild.Id.ToString(CultureInfo.InvariantCulture);
                return $"https://cdn.discordapp.com/splashes/{id}/{guild.SplashHash}.{sfmt}";
            }
            else
            {
                return null;
            }
        }
    }
}
