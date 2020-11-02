using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Unicord.Universal.Misc;
using WamWooWam.Core;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Notifications;
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

        public static Emoji[] Emoji { get; internal set; }
        public static HttpClient HttpClient => _httpClient.Value;

        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());

        public static async Task DownloadToFileAsync(Uri url, StorageFile file)
        {
            CachedFileManager.DeferUpdates(file);

            var resp = await HttpClient.GetAsync(url);
            var source = await resp.Content.ReadAsInputStreamAsync();
            var destination = await file.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(source, destination);

            await CachedFileManager.CompleteUpdatesAsync(file);
        }

        public static async Task DownloadToFileWithProgressAsync(Uri url, StorageFile file, IProgress<HttpProgress> progress)
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

        public static async Task<ContactAnnotationList> GetAnnotationListAsync(ContactAnnotationStore store)
        {
            var lists = await store.FindAnnotationListsAsync();
            var list = lists.FirstOrDefault();
            if (list == null)
            {
                list = await store.CreateAnnotationListAsync();
            }

            return list;
        }

        public static T FindParent<T>(this DependencyObject obj) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent == null)
            {
                return default;
            }

            return parent is T obj1 ? obj1 : parent.FindParent<T>();
        }

        public static List<T> AllChildren<T>(this DependencyObject parent)
        {
            var controlList = new List<T>();
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T c)
                {
                    controlList.Add(c);
                }

                controlList.AddRange(child.AllChildren<T>());
            }
            return controlList;
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
                using (var softwareBmp = await decoder.GetSoftwareBitmapAsync())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBmp);
                    await encoder.FlushAsync();
                }
            }

            return file;
        }

        public static async Task SendFilesWithProgressAsync(DiscordChannel channel, string message, Dictionary<string, IInputStream> files, IProgress<double?> progress)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://discordapp.com/api/v7/channels/{channel.Id}/messages"));
            httpRequestMessage.Headers.Add("Authorization", DSharpPlus.Utilities.GetFormattedToken(channel.Discord));

            var cont = new HttpMultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(message))
            {
                cont.Add(new HttpStringContent(message), "content");
            }

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
            var memberTopRole = member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
            var currentTopRole = current.Roles.OrderByDescending(r => r.Position).FirstOrDefault();

            return (memberTopRole?.Position ?? 0) < (currentTopRole?.Position ?? 0);
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

        public static async Task<List<DiscordEmoji>> GetEmojiAsync(DiscordChannel channel)
        {
            await EnsureEmojiListAsync();

            var guildEmoji = GetAllowedGuildEmoji(channel).ToList();
            guildEmoji.AddRange(DiscordEmoji.UnicodeEmojis.Select(e => DiscordEmoji.FromName(App.Discord, e.Key)));

            return guildEmoji;
        }

        public static async Task<List<EmojiGroup>> GetGroupedEmojiAsync(string text, DiscordChannel channel)
        {
            await EnsureEmojiListAsync();

            var guildEmoji = GetAllowedGuildEmoji(channel);
            var cult = CultureInfo.InvariantCulture.CompareInfo;
            var n = !string.IsNullOrWhiteSpace(text);

            var emojiEnum = Emoji
                    .Where(e => n ? cult.IndexOf(e.Name, text, CompareOptions.IgnoreCase) >= 0 : true)
                    .GroupBy(e => e.Category)
                    .Select(g => new EmojiGroup(g.Key, g))
                    .ToList();

            var list = guildEmoji != null ? guildEmoji.Where(e => n ? cult.IndexOf(e.GetDiscordName(), text, CompareOptions.IgnoreCase) >= 0 : true)
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
            if ((channel.IsPrivate || channel.CurrentPermissions.HasPermission(Permissions.UseExternalEmojis)) && App.Discord.CurrentUser.HasNitro())
            {
                enumerable = App.Discord.Guilds.Values
                    .SelectMany(g => g.Emojis.Values)
                    .OrderBy(g => g.Name);
            }
            else
            {
                enumerable = channel.Guild?.Emojis.Values.OrderBy(g => g.Name);
            }

            return enumerable ?? Enumerable.Empty<DiscordEmoji>();
        }

        private static async Task EnsureEmojiListAsync()
        {
            if (Emoji == null)
            {
                var emojiFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/emoji.json"));
                var emojiList = await FileIO.ReadTextAsync(emojiFile);
                Emoji = await Task.Run(() => JsonConvert.DeserializeObject<Emoji[]>(emojiList));
            }
        }

        internal static ToastNotification GetWindows10Toast(DiscordMessage message, string title, string messageText)
        {
            ToastActionsCustom actions = null;
            var toastBinding = new ToastBindingGeneric()
            {
                Children = { new AdaptiveText() { Text = title, HintStyle = AdaptiveTextStyle.Title }, new AdaptiveText() { Text = messageText } }
            };

            var animated = message.Author.AvatarHash?.StartsWith("a_") ?? false;
            var url = animated ? message.Author.GetAvatarUrl(ImageFormat.Gif, 128) : message.Author.GetAvatarUrl(ImageFormat.Png, 128);
            if (url != null)
            {
                toastBinding.AppLogoOverride = new ToastGenericAppLogo() { Source = url, HintCrop = ToastGenericAppLogoCrop.Circle };
            }

            var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
            if (attach != null)
            {
                double width = attach.Width;
                double height = attach.Height;

                WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 360);

                toastBinding.HeroImage = new ToastGenericHeroImage { Source = attach.ProxyUrl + $"?format=jpeg&width={(int)width}&height={(int)height}" };
            }
            else
            {
                var embed = message.Embeds.FirstOrDefault(em => em.Thumbnail.ProxyUrl != null || em.Image.ProxyUrl != null);
                if (embed != null)
                {
                    toastBinding.HeroImage = new ToastGenericHeroImage { Source = (embed.Thumbnail?.ProxyUrl ?? embed.Image?.ProxyUrl).ToString() };
                }
            }

#if DEBUG
            var replyString = message.Channel is DiscordDmChannel ? $"Reply to @{message.Author.Username}..." : $"Message #{message.Channel.Name}...";
            actions = new ToastActionsCustom()
            {
                Inputs = { new ToastTextBox("tbReply") { PlaceholderContent = replyString } },
                Buttons = { new ToastButton("Reply", "") { ActivationType = ToastActivationType.Background, TextBoxId = "tbReply" } }
            };
#endif


            var toastContent = new ToastContent()
            {
                DisplayTimestamp = message.Timestamp.DateTime,
                Visual = new ToastVisual() { BindingGeneric = toastBinding },
                //HintPeople = new ToastPeople() { RemoteId = "Unicord_" + message.Author.Id.ToString() },
                Launch = $"-channelId={message.ChannelId}",

#if DEBUG
                Actions = actions
#endif

            };

            var str = toastContent.GetContent();

            var doc = new XmlDocument();
            doc.LoadXml(str);

            var notif = new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Allowed,
                Group = message.Channel is DiscordDmChannel ? "Direct Messages" : message.Channel.Name,
                RemoteId = message.Id.ToString()
            };

            return notif;
        }

        public static bool WillShowToast(DiscordMessage message) => SharedTools.WillShowToast(message);
        public static string GetMessageTitle(DiscordMessage message) => SharedTools.GetMessageTitle(message);
        public static string GetMessageContent(DiscordMessage message) => SharedTools.GetMessageContent(message);

        public static bool HasNitro(this DiscordUser user) => user.PremiumType == PremiumType.Nitro || user.PremiumType == PremiumType.NitroClassic;
        public static int UploadLimit(this DiscordUser user) => 
            user.PremiumType == PremiumType.Nitro ? NITRO_UPLOAD_LIMIT :
            user.PremiumType == PremiumType.NitroClassic ? NITRO_CLASSIC_UPLOAD_LIMIT : 
            UPLOAD_LIMIT;
    }

}
