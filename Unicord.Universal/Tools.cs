using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer.Bytes;
using Unicord.Universal.Extensions;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Web.Http;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using static Unicord.Constants;

namespace Unicord.Universal
{
    public static class Tools
    {
        private const int NITRO_UPLOAD_LIMIT = 104_857_600;
        private const int NITRO_CLASSIC_UPLOAD_LIMIT = 52_428_800;
        private const int NITRO_BASIC_UPLOAD_LIMIT = 52_428_800;
        private const int UPLOAD_LIMIT = 26_214_400;

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
                foreach (var c in passwordVault.FindAllByResource(TOKEN_IDENTIFIER))
                {
                    passwordVault.Remove(c);
                }
            }
            catch { }
        }

        public static void AddAccelerator(this UIElement element, VirtualKey key, VirtualKeyModifiers modifiers, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAccelerator")) return;

            var emoteAccelerator = new KeyboardAccelerator() { Key = key, Modifiers = modifiers, ScopeOwner = element };
            emoteAccelerator.Invoked += handler;

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAcceleratorPlacementMode"))
                element.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
            element.KeyboardAccelerators.Add(emoteAccelerator);
        }

        public static void AddAccelerator(this UIElement target, VirtualKey key, VirtualKeyModifiers modifiers)
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAccelerator")) return;

            var emoteAccelerator = new KeyboardAccelerator() { Key = key, Modifiers = modifiers, ScopeOwner = target.FindParent<Page>() };
            target.KeyboardAccelerators.Add(emoteAccelerator);
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

        public static unsafe string GetItemTypeFromExtension(string extension, string fallback = null)
        {
            try
            {
                uint len = 0;
                HRESULT hr;
                if ((hr = PInvoke.AssocQueryString(ASSOCF.ASSOCF_NONE, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, extension, "", null, ref len)) != HRESULT.S_FALSE)
                    hr.ThrowOnFailure();

                char[] buf = new char[len];
                fixed (char* bufPtr = buf)
                {
                    PInvoke.AssocQueryString(ASSOCF.ASSOCF_NONE, ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, extension, "", new PWSTR(bufPtr), ref len)
                        .ThrowOnFailure();

                    return new string(bufPtr);
                }
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

        // adapted from corefx
        // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Array.cs
        public static int BinarySearch<TCollection>(this IList<TCollection> collection, TCollection item)
            where TCollection : IComparable<TCollection>
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

        public static bool HasNitro(this DiscordUser user) => user.PremiumType != 0;
        public static int UploadLimit(this DiscordUser user) => user.PremiumType switch
        {
            PremiumType.NitroClassic => NITRO_CLASSIC_UPLOAD_LIMIT,
            PremiumType.Nitro => NITRO_UPLOAD_LIMIT,
            PremiumType.NitroBasic => NITRO_BASIC_UPLOAD_LIMIT,
            _ => UPLOAD_LIMIT
        };
    }

}

// fun things i have to define because the C# compiler team decided to be dickwads

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}