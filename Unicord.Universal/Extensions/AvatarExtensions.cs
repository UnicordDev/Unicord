using System;
using System.Globalization;
using System.Numerics;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Utilities;
using Windows.Graphics.Display;

namespace Unicord.Universal.Extensions
{
    public static class AvatarExtensions
    {
        private static float logicalDpi;

        static AvatarExtensions()
        {
            var info = DisplayInformation.GetForCurrentView();
            info.DpiChanged += OnDpiChanged;

            logicalDpi = info.LogicalDpi;
        }

        private static void OnDpiChanged(DisplayInformation sender, object args)
        {
            logicalDpi = sender.LogicalDpi;
        }

        public static string GetIconUrl(this DiscordGuild guild, int size)
        {
            if (size < 16 || size > 2048)
                throw new ArgumentOutOfRangeException(nameof(size));

            size = (int)BitOperations.RoundUpToPowerOf2((uint)(size * (logicalDpi / 96.0f)));

            var fmt = WebPHelpers.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
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
            var fmt = WebPHelpers.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
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
            var fmt = WebPHelpers.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;
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


        /// <summary>
        /// Gets the user's avatar URL, in requested format and size.
        /// </summary>
        /// <param name="fmt">Format of the avatar to get.</param>
        /// <param name="size">Maximum size of the avatar. Must be a power of two, minimum 16, maximum 2048.</param>
        /// <returns>URL of the user's avatar.</returns>
        public static string GetAvatarUrl(this DiscordUser user, ushort size = 1024)
        {
            if (size < 16 || size > 2048)
                throw new ArgumentOutOfRangeException(nameof(size));

            size = (ushort)BitOperations.RoundUpToPowerOf2((uint)(size * (logicalDpi / 96.0f)));

            var fmt = WebPHelpers.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;

            var sfmt = "";
            switch (fmt)
            {
                case ImageFormat.Gif:
                    sfmt = "gif";
                    break;

                case ImageFormat.Jpeg:
                    sfmt = "jpg";
                    break;

                case ImageFormat.Png:
                    sfmt = "png";
                    break;

                case ImageFormat.WebP:
                    sfmt = "webp";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fmt));
            }

            var ssize = size.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(user.AvatarHash))
            {
                var id = user.Id.ToString(CultureInfo.InvariantCulture);
                return $"https://cdn.discordapp.com/avatars/{id}/{user.AvatarHash}.{sfmt}?size={ssize}";
            }
            else
            {
                string type;
                if (string.IsNullOrWhiteSpace(user.Discriminator) || user.Discriminator == "0")
                {
                    type = ((user.Id >> 22) % 6).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    type = (int.Parse(user.Discriminator, CultureInfo.InvariantCulture) % 5)
                        .ToString(CultureInfo.InvariantCulture);
                }
                return $"https://cdn.discordapp.com/embed/avatars/{type}.{sfmt}?size={ssize}";
            }
        }
    }
}
