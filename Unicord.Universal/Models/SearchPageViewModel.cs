﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Models
{
    public class SearchMessageGrouping :
        IGrouping<MessageViewModel, MessageViewModel>,
        IReadOnlyCollection<MessageViewModel>,
        IReadOnlyList<MessageViewModel>,
        IEnumerable<MessageViewModel>
    {
        private IReadOnlyList<MessageViewModel> _value;

        public SearchMessageGrouping(MessageViewModel key, IReadOnlyList<MessageViewModel> value)
        {
            Key = key;
            _value = value;
        }

        public MessageViewModel this[int index] => _value[index];

        public MessageViewModel Key { get; }

        public int Count => _value.Count;

        public IEnumerator<MessageViewModel> GetEnumerator() => _value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();
    }

    public class SearchPageViewModel : NotifyPropertyChangeImpl
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DiscordChannel _channel;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private bool _waitingForIndex;
        private bool _isSearching;
        private int _totalMessages;

        public bool IsSearching
        {
            get => _isSearching;
            set => OnPropertySet(ref _isSearching, value);
        }

        public bool WaitingForIndex
        {
            get => _waitingForIndex;
            set => OnPropertySet(ref _waitingForIndex, value);
        }

        public int TotalMessages
        {
            get => _totalMessages;
            set => OnPropertySet(ref _totalMessages, value, nameof(TotalMessages), nameof(TotalMessagesString), nameof(CanGoBack), nameof(CanGoForward));
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => OnPropertySet(ref _currentPage, value, nameof(CurrentPage), nameof(CanGoBack), nameof(CanGoForward));
        }

        public int TotalPages
        {
            get => _totalPages;
            set => OnPropertySet(ref _totalPages, value, nameof(TotalPages), nameof(CanGoBack), nameof(CanGoForward));
        }

        public string TotalMessagesString =>
            TotalMessages.ToString("N0");

        public bool CanGoForward =>
            CurrentPage < TotalPages;

        public bool CanGoBack =>
            CurrentPage > 1;

        public CollectionViewSource ViewSource { get; set; }

        public SearchPageViewModel(DiscordChannel channel)
        {
            _channel = channel;
            ViewSource = new CollectionViewSource();
            ViewSource.IsSourceGrouped = true;
            WaitingForIndex = false;
        }

        public async Task SearchAsync(string content)
        {
            await _semaphore.WaitAsync();

            IsSearching = true;
            ViewSource.Source = Array.Empty<object>();

            try
            {
                DiscordSearchResult result = null;
                if (_channel.Guild != null)
                    result = await App.Discord.SearchAsync(_channel.Guild, content, offset: (CurrentPage - 1) * 25);
                else
                    result = await App.Discord.SearchAsync(_channel, content, offset: (CurrentPage - 1) * 25);

                if (!result.IsIndexed)
                {
                    WaitingForIndex = true;
                    TotalMessages = 0;
                    return;
                }
                else
                {
                    WaitingForIndex = false;
                    TotalMessages = result.TotalResults;
                    TotalPages = (int)Math.Ceiling(result.TotalResults / 25d);
                    ViewSource.Source = result.Messages
                                              .Select(g => new SearchMessageGrouping(new MessageViewModel(g.FirstOrDefault(g => g.Hit.HasValue && g.Hit.Value)), g.Select(v => new MessageViewModel(v)).ToList()))
                                              .ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            finally
            {
                _semaphore.Release();
                IsSearching = false;
            }
        }
    }
}
