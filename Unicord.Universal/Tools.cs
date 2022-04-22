using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Serialization;
using Humanizer;
using Humanizer.Bytes;
using NeoSmart.Unicode;
using Newtonsoft.Json;
using Unicord.Universal.Misc;
//using Unicord.Universal.Native;
using WamWooWam.Core;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;

namespace Unicord.Universal
{
    public static class Tools
    {
        private const int NITRO_UPLOAD_LIMIT = 104_857_600;
        private const int NITRO_CLASSIC_UPLOAD_LIMIT = 52_428_800;
        private const int UPLOAD_LIMIT = 8_388_608;

        public static HttpClient HttpClient => _httpClient.Value;

        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());

        public static string ToFileSizeString(long size)
        {
            if (size < 0)
                return "-" + ToFileSizeString((ulong)-size);
            else
                return ToFileSizeString((ulong)size);
        }

        public static string ToFileSizeString(ulong size)
        {
            return ByteSize.FromBytes(size).ToString("#.##");
        }

        public static Task DownloadToFileAsync(Uri url, StorageFile file)
        {
            return DownloadToFileAsync(url, file, new Progress<HttpProgress>());
        }

        public static Task DownloadToFileAsync(Uri url, StorageFile file, IProgress<double?> progress)
        {
            void HttpProgressToDouble(HttpProgress p)
            {
                if (p.TotalBytesToReceive.HasValue)
                {
                    progress?.Report(p.BytesReceived / (double)p.TotalBytesToReceive.Value);
                }
                else
                {
                    progress?.Report(null);
                }
            }

            return DownloadToFileAsync(url, file, new Progress<HttpProgress>(HttpProgressToDouble));
        }

        public static async Task DownloadToFileAsync(Uri url, StorageFile file, IProgress<HttpProgress> progress)
        {
            CachedFileManager.DeferUpdates(file);

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var resp = await HttpClient.SendRequestAsync(message).AsTask(progress);

            var content = await resp.Content.ReadAsInputStreamAsync();
            var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);

            await RandomAccessStream.CopyAndCloseAsync(content, fileStream);
            await CachedFileManager.CompleteUpdatesAsync(file);
        }

        internal static void ResetPasswordVault()
        {
            try
            {
                var passwordVault = new PasswordVault();
                foreach (var c in passwordVault.FindAllByResource(Constants.TOKEN_IDENTIFIER))
                {
                    passwordVault.Remove(c);
                }
            }
            catch { }
        }

        public static T FindParent<T>(this DependencyObject obj, string controlName = null) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent == null)
            {
                return default;
            }

            return parent is T obj1 && (controlName == null || obj1.Name == controlName) ? obj1 : parent.FindParent<T>(controlName);
        }

        public static T FindChild<T>(this DependencyObject parent, string controlName = null) where T : FrameworkElement
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); ++index)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T t && (controlName == null || t.Name == controlName))
                {
                    return t;
                }
                else if ((child = FindChild<T>(child, controlName)) != null)
                {
                    return child as T;
                }
            }

            return default;
        }

        public static void AddAccelerator(this UIElement element, VirtualKey key, VirtualKeyModifiers modifiers, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAccelerator"))
            {
                var emoteAccelerator = new KeyboardAccelerator() { Key = key, Modifiers = modifiers, ScopeOwner = element };
                emoteAccelerator.Invoked += handler;

                if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAcceleratorPlacementMode"))
                    element.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
                element.KeyboardAccelerators.Add(emoteAccelerator);
            }
        }

        public static void AddAccelerator(this UIElement target, VirtualKey key, VirtualKeyModifiers modifiers)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAccelerator"))
            {
                var emoteAccelerator = new KeyboardAccelerator() { Key = key, Modifiers = modifiers, ScopeOwner = target.FindParent<Page>() };
                target.KeyboardAccelerators.Add(emoteAccelerator);
            }
        }

        public static async Task<StorageFile> GetImageFileFromDataPackage(DataPackageView dataPackageView)
        {
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Strings.RandomString(12)}.png");

            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            using (var bmp = await (await dataPackageView.GetBitmapAsync()).OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(bmp);
                using (var softwareBmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBmp);
                    await encoder.FlushAsync();
                }
            }

            return file;
        }

        public static async Task SendFilesWithProgressAsync(DiscordChannel channel, string message, IEnumerable<IMention> mentions, DiscordMessage replyTo, Dictionary<string, IInputStream> files, IProgress<double?> progress)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://discordapp.com/api/v8/channels/{channel.Id}/messages"));
            httpRequestMessage.Headers.Add("Authorization", DSharpPlus.Utilities.GetFormattedToken(channel.Discord));

            var cont = new HttpMultipartFormDataContent();
            var pld = new RestChannelMessageCreatePayload
            {
                HasContent = !string.IsNullOrWhiteSpace(message),
                Content = message,
                //IsTTS = tts,
                //HasEmbed = embed != null,
                //Embed = embed
            };

            if (mentions != null)
                pld.Mentions = new DiscordMentions(mentions);

            if (replyTo != null)
                pld.MessageReference = new InternalDiscordMessageReference() { messageId = replyTo.Id };

            cont.Add(new HttpStringContent(DiscordJson.SerializeObject(pld)), "payload_json");

            for (var i = 0; i < files.Count; i++)
            {
                var file = files.ElementAt(i);
                cont.Add(new HttpStreamContent(file.Value), $"file{i}", file.Key);
            }

            httpRequestMessage.Content = cont;

            var send = _httpClient.Value.SendRequestAsync(httpRequestMessage);
            send.Progress += new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>((o, e) =>
            {
                if (e.TotalBytesToSend != null)
                    progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
            });

            await send;
        }

        internal static string GetItemTypeFromExtension(string extension, string fallback = null)
        {
            try
            {
                //return Shlwapi.AssocQueryString(ASSOCF.NONE, ASSOCSTR.FRIENDLYAPPNAME, extension, "");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return fallback;
        }

        /// <summary>
        /// Returns true if <paramref name="current"/> is higher in the role hierarchy than <paramref name="member"/>.
        /// </summary>
        /// <param name="current">The current guild member</param>
        /// <param name="member">The guild member to check against</param>
        public static bool CheckRoleHierarchy(DiscordMember current, DiscordMember member)
        {
            if (member == null || current == null)
            {
                return false;
            }

            // i love discord
            var memberTopRole = member.Roles.OrderByDescending(r => r?.Position).FirstOrDefault();
            var currentTopRole = current.Roles.OrderByDescending(r => r?.Position).FirstOrDefault();

            return (memberTopRole?.Position ?? 0) < (currentTopRole?.Position ?? 0);
        }

        public static bool IsAccessible(this DiscordChannel channel)
        {
            if (channel.Type == ChannelType.Category)
                return false;

            if (channel is DiscordDmChannel || channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
                return true;

            if (channel.Guild != null)
            {
                if (channel.Guild.IsOwner)
                    return true;

                var perms = channel.PermissionsFor(channel.Guild.CurrentMember);
                return perms.HasPermission(Permissions.AccessChannels) || perms.HasPermission(Permissions.Administrator);
            }

            return false;
        }

        public static void InvertTheme(ElementTheme requestedTheme, FrameworkElement preview)
        {
            switch (requestedTheme)
            {
                case ElementTheme.Light:
                    preview.RequestedTheme = ElementTheme.Dark;
                    break;
                case ElementTheme.Dark:
                    preview.RequestedTheme = ElementTheme.Light;
                    break;
                default:
                    preview.RequestedTheme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                    break;
            }
        }

        // adapted from corefx
        // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Array.cs
        public static int BinarySearch<TCollection, TOther>(this IList<TCollection> collection, TOther item)
            where TOther : IComparable<TOther> where TCollection : IComparable<TOther>
        {
            var lo = 0;
            var hi = collection.Count - 1;

            while (lo <= hi)
            {
                var i = lo + ((hi - lo) >> 1);
                var c = collection[i].CompareTo(item);

                if (c == 0)
                {
                    return i < 0 ? ~i : i;
                }

                if (c < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo < 0 ? lo : ~lo;
        }

        public static List<DiscordEmoji> GetEmoji(DiscordChannel channel)
        {
            var guildEmoji = GetAllowedGuildEmoji(channel).ToList();
            guildEmoji.AddRange(DiscordEmoji.UnicodeEmojis.Select(e => DiscordEmoji.FromName(App.Discord, e.Key)));

            return guildEmoji;
        }

        public static List<EmojiGroup> GetGroupedEmoji(string text, DiscordChannel channel)
        {
            var guildEmoji = GetAllowedGuildEmoji(channel);
            var cult = CultureInfo.InvariantCulture.CompareInfo;
            var n = !string.IsNullOrWhiteSpace(text);

            var emojiEnum = Emoji.All
                    .Where(e => n ? cult.IndexOf(e.Name, text, CompareOptions.IgnoreCase) >= 0 : true)
                    .GroupBy(e => e.Group)
                    .Select(g => new EmojiGroup(g.Key, g))
                    .ToList();

            var list = guildEmoji != null ? guildEmoji.Where(e => n ? cult.IndexOf(e.DiscordName, text, CompareOptions.IgnoreCase) >= 0 : true)
                .GroupBy(e => App.Discord.Guilds.Values.FirstOrDefault(g => g.Emojis.ContainsKey(e.Id)))
                .OrderBy(g => App.Discord.UserSettings.GuildPositions.IndexOf(g.Key.Id))
                .Select(g => new EmojiGroup(g.Key, g))
                .ToList() : new List<EmojiGroup>();

            list.AddRange(emojiEnum);

            return list;
        }

        private static IEnumerable<DiscordEmoji> GetAllowedGuildEmoji(DiscordChannel channel)
        {
            IEnumerable<DiscordEmoji> enumerable = null;
            var hasNitro = App.Discord.CurrentUser.HasNitro();
            if ((channel.IsPrivate || channel.CurrentPermissions.HasPermission(Permissions.UseExternalEmojis)) && hasNitro)
            {
                enumerable = App.Discord.Guilds.Values
                    .SelectMany(g => g.Emojis.Values)
                    .OrderBy(g => g.Name)
                    .OrderByDescending(g => g.IsAvailable);
            }
            else
            {
                enumerable = channel.Guild?.Emojis.Values.OrderBy(g => g.Name).Where(e => e.IsAnimated ? hasNitro : true);
            }

            return enumerable ?? Enumerable.Empty<DiscordEmoji>();
        }

        public static bool IsText(this DiscordChannel channel) =>
            channel.Type == ChannelType.Text || channel.Type == ChannelType.News || channel.Type == ChannelType.Private || channel.Type == ChannelType.Group;
        public static bool IsVoice(this DiscordChannel channel) =>
            channel.Type == ChannelType.Voice || channel.Type == ChannelType.Stage;

        public static bool HasNitro(this DiscordUser user) => user.PremiumType == PremiumType.Nitro || user.PremiumType == PremiumType.NitroClassic;
        public static int UploadLimit(this DiscordUser user) =>
            user.PremiumType == PremiumType.Nitro ? NITRO_UPLOAD_LIMIT :
            user.PremiumType == PremiumType.NitroClassic ? NITRO_CLASSIC_UPLOAD_LIMIT :
            UPLOAD_LIMIT;
    }

}

// fun things i have to define because the C# compiler team decided to be dickwads

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}