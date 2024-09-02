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
    public static class UserExtensions
    {
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

            var displayInfo = DisplayInformation.GetForCurrentView();
            size = (ushort)BitOperations.RoundUpToPowerOf2((uint)(size * (displayInfo.LogicalDpi / 96.0f)));

            var fmt = Tools.ShouldUseWebP ? ImageFormat.WebP : ImageFormat.Png;

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
