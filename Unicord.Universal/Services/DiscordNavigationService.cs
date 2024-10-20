using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Utils.Synchronization;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.Voice;
using Unicord.Universal.Pages;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    [Flags]
    internal enum NavigationFlags
    {
        /// <summary>
        /// If we're navigating from within another navigation, e.g. guild channel shows the guild channels list, etc.
        /// </summary>
        IsSubNavigation = 1,
        /// <summary>
        /// If we're navigating from the age gate page
        /// </summary>
        IsFromAgeGatePage = 2,
        /// <summary>
        /// Go directly to the friends page, not the last accessed DM channel
        /// </summary>
        GoDirectlyToFriendsPage = 4
    }

    internal enum NavigationType
    {
        Friends,
        Guild,
        Channel
    }

    internal record NavigationEvent(NavigationType Type, ulong? Id, NavigationFlags Flags);

    /// <summary>
    /// A service to handle navigation between channels and servers
    /// </summary>
    internal class DiscordNavigationService : BaseService<DiscordNavigationService>
    {
        private MainPage _mainPage;
        private RootViewModel _rootViewModel;
        private DiscordPage _discordPage;
        private DiscordPageViewModel _discordPageModel;

        private SystemNavigationManager _navigation;
        private Stack<NavigationEvent> _navigationStack;

        private bool _navigating = false;

        private Frame LeftFrame
            => _discordPage?.LeftSidebarFrame;
        private Frame MainFrame
            => _discordPage?.MainFrame ?? _mainPage.RootFrame;

        private NavigationTransitionInfo LeftFrameTransition
            => new DrillInNavigationTransitionInfo();
        private NavigationTransitionInfo MainFrameTransition
            => new EntranceNavigationTransitionInfo();

        private ulong? channelId;
        private ulong? guildId;
        private ulong? previousDmId;
        private NavigationEvent _lastEvent;

        protected override void Initialise()
        {
            _mainPage = Window.Current.Content.FindChild<MainPage>();
            _rootViewModel = RootViewModel.GetForCurrentView();
            if (!_rootViewModel.IsFullFrame)
            {
                _discordPage = Window.Current.Content.FindChild<DiscordPage>();
                _discordPageModel = (DiscordPageViewModel)_discordPage.DataContext;
            }

            _navigationStack = new Stack<NavigationEvent>();
            _navigation = SystemNavigationManager.GetForCurrentView();
            _navigation.BackRequested += OnBackRequested;
        }

        public async Task NavigateAsync(NavigationFlags flags = 0)
        {
            await NavigateAsync(NavigationType.Friends, null, flags);
        }

        public async Task NavigateAsync(DiscordChannel channel, NavigationFlags flags = 0)
        {
            await NavigateAsync(NavigationType.Channel, channel.Id, flags);
        }

        public async Task NavigateAsync(DiscordGuild guild, NavigationFlags flags = 0)
        {
            await NavigateAsync(NavigationType.Guild, guild.Id, flags);
        }

        public async Task NavigateAsync(NavigationType type, ulong? id, NavigationFlags flags)
        {
            // to prevent duplicate navigations            
            if (_navigating)
                return;

            _navigating = true;
            _mainPage.HideCustomOverlay();

            var channelBefore = this.channelId;

            if (flags.HasFlag(NavigationFlags.IsFromAgeGatePage))
                _navigationStack.TryPop(out _); // we're going to overwrite this navigation event

            try
            {
                if (_rootViewModel.IsFullFrame)
                {
                    // TODO: 
                    await DoFullFrameNavigationAsync(type, id, flags);
                    _mainPage.HideConnectingOverlay();
                }
                else
                {
                    if (await DoRegularNavigationAsync(type, id, flags))
                    {
                        if (type == NavigationType.Guild && (channelId != null && channelBefore != channelId))
                        {
                            // rewrite these to channel navigations 
                            type = NavigationType.Channel;
                            id = channelId;
                        }

                        _lastEvent = new NavigationEvent(type, id, flags);
                        _navigationStack.Push(_lastEvent);
                    }
                }
            }
            finally
            {
                _discordPageModel?.UpdateSelection(channelId, guildId);
                _navigating = false;
            }
        }

        private async Task<bool> DoRegularNavigationAsync(NavigationType type, ulong? id, NavigationFlags flags)
        {
            return type switch
            {
                NavigationType.Friends => await NavigateToFriendsAsync(flags),
                NavigationType.Guild => await NavigateToGuildAsync(id.Value, flags),
                NavigationType.Channel => await NavigateToChannelAsync(id.Value, flags),
                _ => throw new InvalidOperationException(),
            };
        }

        private async Task<bool> DoFullFrameNavigationAsync(NavigationType type, ulong? id, NavigationFlags flags)
        {
            return type switch
            {
                NavigationType.Channel => await NavigateToChannelAsync(id.Value, flags),
                _ => throw new InvalidOperationException(),
            };
        }

        private async Task<bool> NavigateToFriendsAsync(NavigationFlags flags)
        {
            Analytics.TrackEvent("DiscordNavigationService_NavigateToFriendsPage");

            if (LeftFrame?.Content is not DMChannelsPage)
                LeftFrame?.Navigate(typeof(DMChannelsPage), null, LeftFrameTransition);

            if (previousDmId != null && (channelId != previousDmId) && !flags.HasFlag(NavigationFlags.GoDirectlyToFriendsPage))
            {
                channelId = previousDmId;

                await NavigateToChannelAsync(previousDmId.Value, NavigationFlags.IsSubNavigation);
            }
            else
            {
                if (MainFrame?.Content is not FriendsPage)
                    MainFrame.Navigate(typeof(FriendsPage), null, MainFrameTransition);

                channelId = null;
                guildId = null;
            }

            previousDmId = null;
            return true;
        }

        private async Task<bool> NavigateToGuildAsync(ulong id, NavigationFlags flags)
        {
            if (!DiscordManager.Discord.Guilds.TryGetValue(id, out var guild) || guild.IsUnavailable)
            {
                await UIUtilities.ShowErrorDialogAsync("ServerUnavailableTitle", "ServerUnavailableMessage");
                return false;
            }

            if (LeftFrame != null && (LeftFrame.Content is not GuildChannelListPage p || p.Guild != guild))
                LeftFrame.Navigate(typeof(GuildChannelListPage), guild, LeftFrameTransition);

            if (!guild.IsSynced)
                await guild.SyncAsync();

            guildId = guild.Id;

            if (!flags.HasFlag(NavigationFlags.IsSubNavigation)
                && !App.LocalSettings.Read(Constants.ENABLE_GUILD_BROWSING, Constants.ENABLE_GUILD_BROWSING_DEFAULT))
            {
                var channelId = App.RoamingSettings.Read($"GuildPreviousChannels::{guild.Id}", 0UL);
                if (!guild.Channels.TryGetValue(channelId, out var channel))
                {
                    if (guild.Threads.TryGetValue(channelId, out var thread))
                        channel = thread;
                }

                if (channel == null || !channel.IsAccessible() || !channel.IsText())
                {
                    channel = guild.Channels.Values
                        .Where(c => c.IsAccessible())
                        .Where(c => c.IsText())
                        .OrderBy(c => c.Position)
                        .FirstOrDefault();
                }

                return await NavigateToChannelAsync(channel.Id, flags | NavigationFlags.IsSubNavigation);
            }

            return true;
        }

        private async Task<bool> NavigateToChannelAsync(ulong id, NavigationFlags flags)
        {
            var channel = TryGetChannel(id);
            var window = WindowingService.Current.GetHandle(_mainPage);

            if (channel.Type == ChannelType.Voice)
                return false;

            if (await WindowingService.Current.ActivateOtherWindowAsync(channel, window))
                return false;

            if (channel is DiscordDmChannel directMessage)
            {
                guildId = null;

                if (!flags.HasFlag(NavigationFlags.IsSubNavigation))
                    LeftFrame?.Navigate(typeof(DMChannelsPage), directMessage, LeftFrameTransition);
            }
            else if (channel.GuildId != null)
            {
                guildId = channel.GuildId;

                if (!flags.HasFlag(NavigationFlags.IsSubNavigation))
                    await NavigateToGuildAsync(channel.GuildId.Value, flags | NavigationFlags.IsSubNavigation);

                if (LeftFrame?.Content is GuildChannelListPage g)
                    g.SetSelectedChannel(channel);
            }

            channelId = channel.Id;

            if (channel.IsNSFW && !flags.HasFlag(NavigationFlags.IsFromAgeGatePage))
            {
                if (!await WindowsHelloManager.VerifyAsync(Constants.VERIFY_NSFW, "VerifyNSFWDisplayReason"))
                    return false;

                if (!App.RoamingSettings.Read($"NSFW_{channel.Id}", false) ||
                    !App.RoamingSettings.Read($"NSFW_All", false))
                {
                    MainFrame.Navigate(typeof(AgeGatePage), channel, MainFrameTransition);
                    return true;
                }
            }

            if (channel is DiscordForumChannel forum)
            {
                MainFrame.Navigate(typeof(ForumChannelPage), forum, MainFrameTransition);
            }
            else
            {
                MainFrame.Navigate(typeof(ChannelPage), channel, MainFrameTransition);
            }

            return true;
        }

        private DiscordChannel TryGetChannel(ulong id)
        {
            if (DiscordManager.Discord.TryGetCachedChannel(id, out var channel))
                return channel;

            if (DiscordManager.Discord.TryGetCachedThread(id, out var thread))
                return thread;

            return null;
        }

        private async void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled) return;

            var overlayService = OverlayService.GetForCurrentView();
            if (overlayService.IsOverlayVisible)
            {
                overlayService.CloseOverlay();
            }

            // top of the stack is always the current channel
            NavigationEvent ev;
            while (_navigationStack.TryPop(out ev) && _lastEvent == ev) ;
            if (ev != null)
            {
                e.Handled = true;
                await NavigateAsync(ev.Type, ev.Id, ev.Flags);
            }
            else
            {
                e.Handled = true;
                await NavigateAsync(NavigationFlags.GoDirectlyToFriendsPage);
            }
        }

        internal async void GoBack()
        {
            NavigationEvent ev;
            while (_navigationStack.TryPop(out ev) && _lastEvent == ev) ;
            if (ev != null)
            {
                await NavigateAsync(ev.Type, ev.Id, ev.Flags);
            }
            else
            {
                await NavigateAsync(NavigationFlags.GoDirectlyToFriendsPage);
            }
        }

        internal void GoForwards()
        {

        }
    }
}
