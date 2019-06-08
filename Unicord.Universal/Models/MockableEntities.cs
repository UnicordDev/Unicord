using System;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models
{
    internal class MockUser : DiscordUser
    {
        internal MockUser(string username, string discriminator)
        {
            Username = username;
            Discriminator = discriminator;
            Discord = App.Discord;
        }

        public override string Username { get; internal set; }
        public override string Discriminator { get; internal set; }
        public override string AvatarUrl => "ms-appx:///Assets/example-avatar.png";
        public override string NonAnimatedAvatarUrl => "ms-appx:///Assets/example-avatar.png";
    }

    internal class MockChannel : DiscordChannel
    {
        internal MockChannel(string name, ChannelType type, string topic)
        {
            Name = name;
            Type = type;
            Topic = topic;
            Discord = App.Discord;
        }

        public override string Name { get; internal set; }
        public override ChannelType Type { get; internal set; }
        public override string Topic { get; internal set; }
    }

    internal class MockMessage : DiscordMessage
    {
        internal MockMessage(string content, DiscordUser author, DiscordChannel channel = null, DateTimeOffset timestamp = default)
        {
            Content = content;
            Author = author;
            Channel = channel;
            Timestamp = timestamp;
            Discord = App.Discord;
        }

        public override DiscordUser Author { get; internal set; }
        public override string Content { get; internal set; }

        public override DiscordChannel Channel { get; }
        public override DateTimeOffset Timestamp { get; }

        internal void NotifyAllChanged()
        {
            InvokePropertyChanged("");
            InvokePropertyChanged(nameof(Author));
            InvokePropertyChanged(nameof(Content));
            InvokePropertyChanged(nameof(Channel));
            InvokePropertyChanged(nameof(Timestamp));
        }
    }
}
