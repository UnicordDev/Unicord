using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
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
        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());
        public static HttpClient HttpClient => _httpClient.Value;

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

        public static async Task<ContactAnnotationList> GetAnnotationlistAsync(ContactAnnotationStore store)
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
            var httpRequestMessage
                = new HttpRequestMessage(HttpMethod.Post, new Uri("https://discordapp.com/api/v7" + string.Format("/channels/{0}/messages", channel.Id)));
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
                progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
            });

            await send;
        }

        /// <summary>
        /// Returns true if <paramref name="current"/> is higher in the role heirarchy than <paramref name="member"/>.
        /// </summary>
        /// <param name="current">The current guild member</param>
        /// <param name="member">The guild member to check against</param>
        public static bool CheckRoleHeirarchy(DiscordMember current, DiscordMember member)
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

        // adapted from corefx
        // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Array.cs
        public static int BinarySearch<T>(this IList<T> collection, DiscordChannel channel) where T : IComparable<DiscordChannel>
        {
            var lo = 0;
            var hi = collection.Count - 1;

            while (lo <= hi)
            {
                var i = lo + ((hi - lo) >> 1);
                var c = collection[i].CompareTo(channel);

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
                var width = attach.Width;
                var height = attach.Height;

                WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 360);

                toastBinding.HeroImage = new ToastGenericHeroImage { Source = attach.ProxyUrl + $"?format=jpeg&width={width}&height={height}" };
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
    }

}
