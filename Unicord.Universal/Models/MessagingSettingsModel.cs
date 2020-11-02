using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unicord.Universal.Controls.Messages;
using Windows.UI.Xaml;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class MessageStyle
    {
        public string Key { get; set; }
        public Style Value { get; set; }
        public DiscordMessage ExampleMessage { get; set; }
    }

    class MessagingSettingsModel : NotifyPropertyChangeImpl
    {
        public MessagingSettingsModel()
        {
            AvailableMessageStyles = new ObservableCollection<MessageStyle>();
            FindMessageStyles(App.Current.Resources, AvailableMessageStyles);
            RegenerateMessage();
        }

        internal void RegenerateMessage()
        {
            var user = App.Discord.CreateMockUser("ExampleUser", "ABCD");
            var channel = App.Discord.CreateMockChannel("text", ChannelType.Text, "This is an example channel.");
            ExampleMessage = App.Discord.CreateMockMessage("This is an example message!", user, channel, DateTime.Now.Subtract(TimeSpan.FromMinutes(3)));
        }

        private void FindMessageStyles(ResourceDictionary baseDict, IList<MessageStyle> availableMesssageStyles)
        {
            foreach (var dict in baseDict.MergedDictionaries)
            {
                FindMessageStyles(dict, availableMesssageStyles);
            }

            foreach (var resource in baseDict)
            {
                if (resource.Value is Style s && s.TargetType == typeof(MessageControl))
                {
                    availableMesssageStyles.Add(new MessageStyle() { Key = resource.Key.ToString(), Value = s, ExampleMessage = ExampleMessage });
                }
            }
        }

        public DiscordMessage ExampleMessage { get; set; }

        public MessageStyle SelectedMessageStyle
        {
            get
            {
                var key = App.LocalSettings.Read(MESSAGE_STYLE_KEY, MESSAGE_STYLE_DEFAULT);
                var style = AvailableMessageStyles.FirstOrDefault(s => s.Key == key);
                if (style == null) // additional validation to make sure the style exists
                {
                    // if this doesn't work something's fucked big time
                    App.LocalSettings.Save(MESSAGE_STYLE_KEY, MESSAGE_STYLE_DEFAULT);
                    style = AvailableMessageStyles.FirstOrDefault(s => s.Key == MESSAGE_STYLE_DEFAULT);
                }

                return style;
            }

            set => App.LocalSettings.Save(MESSAGE_STYLE_KEY, value.Key);
        }

        public ObservableCollection<MessageStyle> AvailableMessageStyles { get; set; }

        public bool EnableSpoilers
        {
            get => App.RoamingSettings.Read(ENABLE_SPOILERS, true);
            set => App.RoamingSettings.Save(ENABLE_SPOILERS, value);
        }

        public int TimestampStyle
        {
            get => (int)App.RoamingSettings.Read(TIMESTAMP_STYLE, Unicord.TimestampStyle.Absolute);
            set => App.RoamingSettings.Save(TIMESTAMP_STYLE, (TimestampStyle)value);
        }

        public bool AutoPlayGifs
        {
            get => App.RoamingSettings.Read(GIF_AUTOPLAY, true);
            set => App.RoamingSettings.Save(GIF_AUTOPLAY, value);
        }

        public bool WarnUnsafeLinks
        {
            get => App.RoamingSettings.Read(WARN_UNSAFE_LINKS, true);
            set => App.RoamingSettings.Save(WARN_UNSAFE_LINKS, value);
        }
    }
}
