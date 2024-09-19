using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;

using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using System.Linq;

namespace Unicord.Universal.Models
{
    public class ChannelEditViewModel : ViewModelBase
    {
        public ChannelEditViewModel(DiscordChannel channel)
        {
            Channel = channel;

            Name = channel.Name;
            Topic = channel.Topic;
            NSFW = channel.IsNSFW;
            Userlimit = channel.UserLimit.GetValueOrDefault();
            Bitrate = channel.Bitrate.GetValueOrDefault() / 1000;

            // this is awful and horrible and bad
            PermissionOverwrites = new ObservableCollection<NavigationViewItem>();

            foreach (var overwrite in channel.PermissionOverwrites.OrderBy(o => o.Type))
            {
                var content = "";
                var icon = new SymbolIcon();
                
                if (overwrite.Type == OverwriteType.Member)
                {
                    icon.Symbol = Symbol.Contact;
                    content = channel.Guild.Members.TryGetValue(overwrite.Id, out var member)
                        ? $"@{member.Username}#{member.Discriminator}"
                        : $"Unknown Member {overwrite.Id}";
                }
                else if(overwrite.Type == OverwriteType.Role)
                {
                    var role = channel.Guild.Roles[overwrite.Id];
                    icon.Symbol = (Symbol)57704;
                    content = role.Name;
                }

                PermissionOverwrites.Add(new NavigationViewItem() { Tag = overwrite, Content = content, Icon = icon });
            }
        }

        public DiscordChannel Channel { get; }

        public string Name { get; set; }

        public bool IsText => Channel.Type == ChannelType.Text;
        public string Topic { get; set; }
        public bool NSFW { get; set; }

        public bool IsVoice => Channel.Type == ChannelType.Voice;
        public int Userlimit { get; set; }
        public int Bitrate { get; set; }

        // BUGBUG: this is an MVVM travesty
        public ObservableCollection<NavigationViewItem> PermissionOverwrites { get; set; }

        public Task SaveChangesAsync()
        {
            Analytics.TrackEvent("ChannelEditViewModel_SaveChanges");

            if (IsText)
            {
                return Channel.ModifyAsync(m =>
                {
                    m.Name = Name;
                    m.Topic = Topic;
                    m.Nsfw = NSFW;
                });
            }
            if (IsVoice)
            {
                return Channel.ModifyAsync(m =>
                {
                    m.Name = Name;
                    m.Userlimit = (int)Userlimit;
                    m.Bitrate = (int)Bitrate * 1000;
                });
            }

            return Task.CompletedTask;
        }
    }
}
