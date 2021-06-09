using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unicord.Universal.Models.Messages
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        private bool _isLoaded;
        private static int _handlers;
        private SynchronizationContext _context;

        public MessageViewModel(DiscordMessage discordMessage)
        {
            _context = SynchronizationContext.Current;

            Message = discordMessage;
            Embeds = new ObservableCollection<DiscordEmbed>(Message.Embeds);
            Attachments = new ObservableCollection<DiscordAttachment>(Message.Attachments);
            Stickers = new ObservableCollection<DiscordSticker>(Message.Stickers);
            Reactions = new ObservableCollection<DiscordReaction>(Message.Reactions);
            Components = new ObservableCollection<DiscordComponent>(Message.Components);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DiscordMessage Message { get; }

        public DiscordMessage ReferencedMessage 
            => Message.ReferencedMessage;
        public DiscordChannel Channel 
            => Message.Channel;
        public DiscordUser Author 
            => Message.Author;
        public DateTimeOffset Timestamp 
            => Message.Timestamp;
        public string Content 
            => Message.Content;

        public ObservableCollection<DiscordEmbed> Embeds { get; }
        public ObservableCollection<DiscordAttachment> Attachments { get; }
        public ObservableCollection<DiscordSticker> Stickers { get; }
        public ObservableCollection<DiscordReaction> Reactions { get; }
        public ObservableCollection<DiscordComponent> Components { get; }

        internal void OnLoaded()
        {
            if (App.Discord != null && !_isLoaded)
            {
                _isLoaded = true;
                _handlers++;

                Message.PropertyChanged += OnPropertyChanged;
                App.Discord.MessageUpdated += OnMessageUpdated;
                App.Discord.MessageReactionAdded += OnReactionAdded;
                App.Discord.MessageReactionRemoved += OnReactionRemoved;
                App.Discord.MessageReactionsCleared += OnReactionsCleared;
                App.Discord.MessageReactionRemovedEmoji += OnReactionGroupRemoved;
            }
        }

        internal void OnUnloaded()
        {
            if (App.Discord != null && _isLoaded)
            {
                _isLoaded = false;
                _handlers--;

                Message.PropertyChanged -= OnPropertyChanged;
                App.Discord.MessageUpdated -= OnMessageUpdated;
                App.Discord.MessageReactionAdded -= OnReactionAdded;
                App.Discord.MessageReactionRemoved -= OnReactionRemoved;
                App.Discord.MessageReactionsCleared -= OnReactionsCleared;
                App.Discord.MessageReactionRemovedEmoji -= OnReactionGroupRemoved;
                Logger.Log(_handlers);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private Task OnReactionAdded(MessageReactionAddEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private Task OnReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private Task OnReactionsCleared(MessageReactionsClearEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            _context.Post((o) => ((MessageViewModel)o).Reactions.Clear(), this);
            return Task.CompletedTask;
        }

        private Task OnReactionGroupRemoved(MessageReactionRemoveEmojiEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            UpdateReactions();
            return Task.CompletedTask;
        }

        private Task OnMessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Message.Id != Message.Id)
                return Task.CompletedTask;

            if (Embeds.SequenceEqual(e.Message.Embeds))
                return Task.CompletedTask;

            _context.Post((o) =>
            {
                Embeds.Clear();
                foreach (var embed in e.Message.Embeds)
                {
                    Embeds.Add(embed);
                }
            }, null);

            return Task.CompletedTask;
        }

        private void UpdateReactions()
        {
            foreach (var reaction in Message.Reactions)
            {
                if (!Reactions.Contains(reaction))
                    _context.Post((o) => Reactions.Add(reaction), null);
            }

            foreach (var reaction in Reactions.ToList())
            {
                if (!Message.Reactions.Contains(reaction))
                    _context.Post((o) => Reactions.Remove(reaction), null);
            }
        }
    }
}
