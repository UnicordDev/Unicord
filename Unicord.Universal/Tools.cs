using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Web.Http;

namespace Unicord.Universal
{
    public static class Tools
    {
        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());
        public static HttpClient HttpClient => _httpClient.Value;

        public static async Task DownloadToFileAsync(Uri url, StorageFile file)
        {
            var resp = await HttpClient.GetAsync(url);
            var source = await resp.Content.ReadAsInputStreamAsync();
            var destination = await file.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(source, destination);
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
                return default;

            return parent is T obj1 ? obj1 : parent.FindParent<T>();
        }

        public static List<T> AllChildren<T>(this DependencyObject parent)
        {
            var controlList = new List<T>();
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T c)
                    controlList.Add(c);

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
                    return t;
                else if ((child = FindChild<T>(child, controlName)) != default)
                    return child as T;
            }

            return default;
        }

        public static async Task SendFileWithProgressAsync(DiscordChannel channel, BaseDiscordClient client, string message, IInputStream file, string fileName, IProgress<double?> progress)
        {
            var httpRequestMessage
                = new HttpRequestMessage(HttpMethod.Post, new Uri("https://discordapp.com/api/v7" + string.Format("/channels/{0}/messages", channel.Id)));
            httpRequestMessage.Headers.Add("Authorization", DSharpPlus.Utilities.GetFormattedToken(client));

            var cont = new HttpMultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(message))
            {
                cont.Add(new HttpStringContent(message), "content");
            }

            cont.Add(new HttpStreamContent(file), "file", fileName);

            httpRequestMessage.Content = cont;

            var send = _httpClient.Value.SendRequestAsync(httpRequestMessage);
            send.Progress += new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>((o, e) =>
            {
                progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
            });

            await send;
        }

        public static async Task SendFilesWithProgressAsync(DiscordChannel channel, BaseDiscordClient client, string message, Dictionary<string, IInputStream> files, IProgress<double?> progress)
        {
            var httpRequestMessage
                = new HttpRequestMessage(HttpMethod.Post, new Uri("https://discordapp.com/api/v7" + string.Format("/channels/{0}/messages", channel.Id)));
            httpRequestMessage.Headers.Add("Authorization", DSharpPlus.Utilities.GetFormattedToken(client));

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

        public static bool CheckRoleHeirarchy(DiscordMember _member, DiscordMember _current)
        {
            if (_member == null || _current == null)
                return false;

            return _member.Roles?.OrderBy(r => r?.Position).FirstOrDefault()?.Position > _current.Roles?.OrderBy(r => r?.Position).FirstOrDefault()?.Position;
        }

        public static Storyboard GetStoryboard(this FrameworkElement element, string name, string message = null)
        {
            if (!(element.Resources[name] is Storyboard resource))
            {
                if (message == null)
                    message = string.Format("Storyboard '{0}' cannot be found! Check the default Generic.xaml.", name);
                throw new NullReferenceException(message);
            }
            return resource;
        }

        public static CompositeTransform GetCompositeTransform(this FrameworkElement element, string message = null)
        {
            if (!(element.RenderTransform is CompositeTransform renderTransform))
            {
                if (message == null)
                    message = string.Format("{0}'s RenderTransform should be a CompositeTransform! Check the default Generic.xaml.", element.Name);
                throw new NullReferenceException(message);
            }
            return renderTransform;
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
                    toastBinding.HeroImage = new ToastGenericHeroImage { Source = (embed.Thumbnail?.ProxyUrl ?? embed.Image?.ProxyUrl).ToString() };
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
