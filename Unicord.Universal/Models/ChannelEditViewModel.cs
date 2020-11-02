using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;

namespace Unicord.Universal.Models
{
    public class ChannelEditViewModel : ViewModelBase
    {
        private DiscordChannel _channel;

        public ChannelEditViewModel(DiscordChannel channel)
        {
            _channel = channel;

            Name = channel.Name;
            Topic = channel.Topic;
            NSFW = channel.IsNSFW;
            Userlimit = channel.UserLimit;
            Bitrate = channel.Bitrate / 1000;
        }

        public string Name { get; set; }

        public bool IsText => _channel.Type == ChannelType.Text;
        public string Topic { get; set; }
        public bool NSFW { get; set; }

        public bool IsVoice => _channel.Type == ChannelType.Voice;
        public int Userlimit { get; set; }
        public int Bitrate { get; set; }

        public Task SaveChangesAsync()
        {
            Analytics.TrackEvent("ChannelEditViewModel_SaveChanges");

            if (IsText)
            {
                return _channel.ModifyAsync(m =>
                {
                    m.Name = Name;
                    m.Topic = Topic;
                    m.Nsfw = NSFW;
                });
            }
            if (IsVoice)
            {
                return _channel.ModifyAsync(m =>
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
