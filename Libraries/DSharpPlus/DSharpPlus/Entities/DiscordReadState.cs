using System;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    public class DiscordReadState : PropertyChangedBase
    {
        public static DiscordReadState Default { get; } = new DiscordReadState();

        private int _mentionCount;
        private ulong _lastMessageId;
        private DateTimeOffset _lastPinTimestamp;

        [JsonIgnore]
        internal DiscordClient Discord { get; set; }

        [JsonProperty("id")]
        public ulong Id { get; internal set; }

        [JsonIgnore]
        public bool Unread
        {
            get
            {
                // this shit should never happen but apparently it does sometimes, don't question it
                if (Id == 0)
                    return false;

                var channel = Discord?.InternalGetCachedChannel(Id);
                if (channel == null)
                    return false;

                if (channel.Type == ChannelType.Text || channel.Type == ChannelType.Private || channel.Type == ChannelType.Group || channel.Type == ChannelType.News)
                {
                    if (channel.Muted)
                        return false;

                    if (channel.Type == ChannelType.Private || channel.Type == ChannelType.Group)
                    {
                        return MentionCount > 0;
                    }

                    return (MentionCount > 0 || (channel.LastMessageId != 0 ? channel.LastMessageId > _lastMessageId : false));
                }
                else
                {
                    return false;
                }
            }
        }

        [JsonProperty("mention_count")]
        public int MentionCount { get => _mentionCount; internal set { OnPropertySet(ref _mentionCount, value); InvokePropertyChanged(nameof(Unread)); } }

        [JsonProperty("last_message_id")]
        public ulong LastMessageId { get => _lastMessageId; internal set { OnPropertySet(ref _lastMessageId, value); InvokePropertyChanged(nameof(Unread)); } }

        [JsonProperty("last_pin_timestamp")]
        public DateTimeOffset LastPinTimestamp { get => _lastPinTimestamp; internal set => OnPropertySet(ref _lastPinTimestamp, value); }
    }
}