using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using Unicord.Universal.Voice;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    internal class NavigationEvent
    {
        public object args;
        public Action<NavigationEvent> action;
    }

    /// <summary>
    /// A service to handle navigation between channels and servers
    /// </summary>
    internal class DiscordNavigationService : BaseService<DiscordNavigationService>
    {
        private MainPage _mainPage;
        private DiscordPage _discordPage;
        private DiscordPageModel _discordPageModel;
        private SystemNavigationManager _navigation;
        private Stack<NavigationEvent> _navigationStack;

        protected override void Initialise()
        {
            base.Initialise();

            _mainPage = Window.Current.Content.FindChild<MainPage>();
            _discordPage = Window.Current.Content.FindChild<DiscordPage>();
            _discordPageModel = _discordPage.DataContext as DiscordPageModel;

            _navigationStack = new Stack<NavigationEvent>();
            _navigation = SystemNavigationManager.GetForCurrentView();
            _navigation.BackRequested += OnBackRequested;
        }

        internal async Task NavigateAsync(DiscordChannel channel, bool skipPreviousDm = false)
        {
            var page = _discordPage.MainFrame.Content as ChannelPage;
            if (channel == null)
            {
                Analytics.TrackEvent("DiscordNavigationService_NavigateToFriendsPage");

                _discordPageModel.SelectedGuild = null;
                _discordPageModel.IsFriendsSelected = true;

                if (_discordPageModel.PreviousDM != null && (page?.ViewModel.Channel != _discordPageModel.PreviousDM) && !skipPreviousDm)
                {
                    _discordPageModel.SelectedDM = _discordPageModel.PreviousDM;
                    _discordPage.MainFrame.Navigate(typeof(ChannelPage), _discordPageModel.PreviousDM);
                    _discordPage.SidebarFrame.Navigate(typeof(DMChannelsPage), _discordPageModel.PreviousDM, new DrillInNavigationTransitionInfo());
                }
                else if (page != null || !(_discordPage.SidebarFrame.Content is DMChannelsPage))
                {
                    _discordPageModel.PreviousDM = null;
                    _discordPage.MainFrame.Navigate(typeof(FriendsPage));
                    _discordPage.SidebarFrame.Navigate(typeof(DMChannelsPage), null, new DrillInNavigationTransitionInfo());
                }

                return;
            }

            if (_discordPageModel.CurrentChannel != channel && channel.Type != ChannelType.Voice)
            {
                Analytics.TrackEvent("DiscordNavigationService_NavigateToTextChannel");

                _discordPageModel.Navigating = true;
                _discordPage.CloseSplitPane(); // pane service?

                _discordPageModel.SelectedGuild = null;
                _discordPageModel.SelectedDM = null;
                _discordPageModel.IsFriendsSelected = false;

                if (await WindowingService.Current.ActivateOtherWindowAsync(channel))
                    return;

                if (channel is DiscordDmChannel dm)
                {
                    _discordPageModel.SelectedDM = dm;
                    _discordPageModel.PreviousDM = dm;
                    _discordPageModel.IsFriendsSelected = true;
                    _discordPage.SidebarFrame.Navigate(typeof(DMChannelsPage), channel, new DrillInNavigationTransitionInfo());
                }
                else if (channel.Guild != null)
                {
                    _discordPageModel.SelectedGuild = channel.Guild;

                    if (!(_discordPage.SidebarFrame.Content is GuildChannelListPage p) || p.Guild != channel.Guild)
                        _discordPage.SidebarFrame.Navigate(typeof(GuildChannelListPage), channel.Guild, new DrillInNavigationTransitionInfo());
                }

                if (channel.IsNSFW)
                {
                    var loader = ResourceLoader.GetForViewIndependentUse();
                    if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_NSFW, loader.GetString("VerifyNSFWDisplayReason")))
                    {
                        if (App.RoamingSettings.Read($"NSFW_{channel.Id}", false) == false || !App.RoamingSettings.Read($"NSFW_All", false))
                        {
                            _discordPage.MainFrame.Navigate(typeof(ChannelWarningPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                        }
                        else
                        {
                            _discordPage.MainFrame.Navigate(typeof(ChannelPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                        }
                    }
                }
                else
                {
                    _discordPage.MainFrame.Navigate(typeof(ChannelPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                }

                _discordPageModel.Navigating = false;
            }
            else if (channel?.Type == ChannelType.Voice)
            {
                Analytics.TrackEvent("DiscordNavigationService_NavigateToVoiceChannel");

                try
                {
                    var voice = new VoiceConnectionModel(channel);
                    _discordPageModel.VoiceModel = voice;
                    await voice.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    await UIUtilities.ShowErrorDialogAsync("Failed to connect to voice!", ex.Message);
                }
            }

            if (_mainPage.IsOverlayShown)
                _mainPage.HideConnectingOverlay();
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_navigationStack.TryPop(out var ev))
            {
                ev.action(ev);
            }
        }
    }
}
