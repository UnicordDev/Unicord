using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Unicord.Universal.Integration
{
    internal static class JumpListManager
    {
        public static async Task AddToListAsync(DiscordChannel channel)
        {
            try
            {
                if (JumpList.IsSupported())
                {
                    var list = await JumpList.LoadCurrentAsync();
                    var data = ApplicationData.Current.LocalFolder;
                    var folder = await data.CreateFolderAsync("recents", CreationCollisionOption.OpenIfExists);

                    string title = null;
                    string group = null;
                    StorageFile file = null;

                    var arguments = $"-channelId={channel.Id}";

                    if (channel.Guild != null)
                    {
                        group = "ms-resource:RecentChannelsJumpListGroup";
                        title = $"#{channel.Name} - {channel.Guild.Name}";

                        file = await folder.CreateFileAsync($"server-{channel.Guild.IconHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(channel.Guild.IconUrl + "?size=64"), file);
                    }
                    else if (channel is DiscordDmChannel dm && dm.Type == ChannelType.Private)
                    {
                        group = "ms-resource:RecentPeopleJumpListGroup";
                        title = $"@{dm.Recipient.Username}";

                        file = await folder.CreateFileAsync($"user-{dm.Recipient.AvatarHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(dm.Recipient.GetAvatarUrl(ImageFormat.Png, 64)), file);
                    }

                    if (group != null && arguments != null && file != null)
                    {
                        var item = list.Items.FirstOrDefault(i => i.Arguments == arguments);
                        if (item == null)
                        {
                            item = JumpListItem.CreateWithArguments(arguments, title);
                            item.GroupName = group;
                            item.Logo = new Uri($"ms-appdata:///local/recents/{file.Name}");
                            list.Items.Add(item);
                        }
                        else
                        {
                            list.Items.Remove(item);
                            list.Items.Insert(0, item);
                        }

                        if (list.Items.Count > 10)
                        {
                            list.Items.Remove(list.Items.Last());
                        }
                    }

                    await list.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
