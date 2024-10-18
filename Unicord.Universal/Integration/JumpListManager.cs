using DSharpPlus;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Unicord.Universal.Integration
{
    internal class JumpListManager
    {
        public static async Task AddToListAsync(ChannelViewModel channel)
        {
            channel = channel ?? throw new ArgumentNullException(nameof(channel));

            if (!JumpList.IsSupported())
                return;

            var logger = Logger.GetLogger<JumpListManager>();
            var list = await JumpList.LoadCurrentAsync();
            try
            {
                var data = ApplicationData.Current.LocalFolder;
                var folder = await data.CreateFolderAsync("recents", CreationCollisionOption.OpenIfExists);

                string title = null;
                string group = null;
                StorageFile file = null;

                var arguments = $"-channelId={channel.Id}";

                if (channel.Guild != null && channel.Channel.IsText())
                {
                    group = "ms-resource:///Resources/RecentChannelsJumpListGroup";
                    title = $"#{channel.Name} - {channel.Guild.Name}";

                    if (!string.IsNullOrWhiteSpace(channel.Guild.Guild.IconHash))
                    {
                        file = await folder.CreateFileAsync($"server-{channel.Guild.Guild.IconHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(channel.Guild.IconUrl + "?size=128"), file);
                    }
                }
                else if (channel.ChannelType == ChannelType.Private)
                {
                    var recipient = channel.Recipient;
                    group = "ms-resource:///Resources/RecentPeopleJumpListGroup";
                    title = $"{recipient.DisplayName}";

                    if (!string.IsNullOrWhiteSpace(recipient.User.AvatarHash))
                    {
                        file = await folder.CreateFileAsync($"user-{recipient.User.AvatarHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(recipient.User.GetAvatarUrl(ImageFormat.Png, 128)), file);
                    }
                }
                else
                {
                    return;
                }

                if (group != null && arguments != null)
                {
                    logger.LogInformation("Adding jump list item for {Channel}, group {Group}", channel.Channel, group);

                    var item = list.Items.FirstOrDefault(i => i.Arguments == arguments);
                    if (item == null)
                    {
                        item = JumpListItem.CreateWithArguments(arguments, title);
                        item.GroupName = group;
                        if (file != null)
                            item.Logo = new Uri($"ms-appdata:///local/recents/{file.Name}");
                        else
                            item.Logo = new Uri($"ms-appx:///Assets/example-avatar.png");

                        list.Items.Add(item);
                    }
                    else
                    {
                        list.Items.Remove(item);
                        list.Items.Insert(0, item);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add jump list item for {Channel}", channel.Channel);
            }
            finally
            {
                while (list.Items.Count > 10)
                    list.Items.Remove(list.Items.Last());

                await list.SaveAsync();
            }
        }
    }
}
