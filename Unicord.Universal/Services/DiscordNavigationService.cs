using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
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
        private DiscordPage _page;
        private DiscordPageModel _pageModel;
        private SystemNavigationManager _navigation;
        private Stack<NavigationEvent> _navigationStack;

        protected override void Initialise()
        {
            base.Initialise();

            _page = Window.Current.Content.FindChild<DiscordPage>();
            _pageModel = _page.DataContext as DiscordPageModel;

            _navigationStack = new Stack<NavigationEvent>();
            _navigation = SystemNavigationManager.GetForCurrentView();
            _navigation.BackRequested += OnBackRequested;
        }

        internal async Task NavigateAsync(DiscordChannel channel, bool skipPreviousDm = false)
        {
            var page = _page.MainFrame.Content as ChannelPage;

            if (channel == null)
            {
                _pageModel.SelectedGuild = null;
                _pageModel.IsFriendsSelected = true;

                if (_pageModel.PreviousDM != null && (page?.ViewModel.Channel != _pageModel.PreviousDM) && !skipPreviousDm)
                {
                    _pageModel.SelectedDM = _pageModel.PreviousDM;
                    _page.MainFrame.Navigate(typeof(ChannelPage), _pageModel.PreviousDM);
                    _page.SidebarFrame.Navigate(typeof(DMChannelsPage), _pageModel.PreviousDM, new DrillInNavigationTransitionInfo());
                }
                else if (page != null || !(_page.SidebarFrame.Content is DMChannelsPage))
                {
                    _pageModel.PreviousDM = null;
                    _page.MainFrame.Navigate(typeof(FriendsPage));
                    _page.SidebarFrame.Navigate(typeof(DMChannelsPage), null, new DrillInNavigationTransitionInfo());
                }

                return;
            }

            if (_pageModel.CurrentChannel != channel && channel.Type != ChannelType.Voice)
            {
                _pageModel.Navigating = true;
                _page.CloseSplitPane(); // pane service?

                _pageModel.SelectedGuild = null;
                _pageModel.SelectedDM = null;
                _pageModel.IsFriendsSelected = false;

                if (await WindowManager.ActivateOtherWindow(channel))
                    return;

                if (channel is DiscordDmChannel dm)
                {
                    _pageModel.SelectedDM = dm;
                    _pageModel.PreviousDM = dm;
                    _pageModel.IsFriendsSelected = true;
                    _page.SidebarFrame.Navigate(typeof(DMChannelsPage), channel, new DrillInNavigationTransitionInfo());
                }
                else if (channel.Guild != null)
                {
                    _pageModel.SelectedGuild = channel.Guild;

                    if (!(_page.SidebarFrame.Content is GuildChannelListPage p) || p.Guild != channel.Guild)
                        _page.SidebarFrame.Navigate(typeof(GuildChannelListPage), channel.Guild, new DrillInNavigationTransitionInfo());
                }

                if (channel.IsNSFW)
                {
                    var loader = ResourceLoader.GetForViewIndependentUse();
                    if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_NSFW, loader.GetString("VerifyNSFWDisplayReason")))
                    {
                        if (App.RoamingSettings.Read($"NSFW_{channel.Id}", false) == false || !App.RoamingSettings.Read($"NSFW_All", false))
                        {
                            _page.MainFrame.Navigate(typeof(ChannelWarningPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                        }
                        else
                        {
                            _page.MainFrame.Navigate(typeof(ChannelPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                        }
                    }
                }
                else
                {
                    _page.MainFrame.Navigate(typeof(ChannelPage), channel/*, info ?? new SlideNavigationTransitionInfo()*/);
                }

                _pageModel.Navigating = false;
            }
            else if (channel?.Type == ChannelType.Voice)
            {
                try
                {
                    var voice = new VoiceConnectionModel(channel);
                    _pageModel.VoiceModel = voice;
                    await voice.ConnectAsync();
                }
                catch (Exception ex)
                {
                    await UIUtilities.ShowErrorDialogAsync("Failed to connect to voice!", ex.Message);
                }
            }
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
