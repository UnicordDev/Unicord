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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    /// <summary>
    /// A service to handle navigation between channels and servers
    /// </summary>
    internal class DiscordNavigationService : BaseService<DiscordNavigationService>
    {
        private DiscordPage _page;
        private DiscordPageModel _pageModel;

        protected override void Initialise()
        {
            base.Initialise();

            _page = Window.Current.Content.FindChild<DiscordPage>();
            _pageModel = _page.DataContext as DiscordPageModel;
        }

        internal async Task NavigateAsync(DiscordChannel channel)
        {
            if (_pageModel.CurrentChannel != channel)
            {
                _pageModel.Navigating = true;
                _page.CloseSplitPane(); // pane service?

                _pageModel.SelectedGuild = null;
                _pageModel.SelectedDM = null;

                // friendsItem.IsSelected = false;

                if (channel == null)
                {
                    // friendsItem.IsSelected = true;
                    _page.SidebarFrame.Navigate(typeof(DMChannelsPage), channel, new DrillInNavigationTransitionInfo());
                    _page.MainFrame.Navigate(typeof(FriendsPage));

                    return;
                }

                if (await WindowManager.ActivateOtherWindow(channel))
                    return;

                if (channel is DiscordDmChannel dm)
                {
                    _pageModel.SelectedDM = dm;
                    // friendsItem.IsSelected = true;

                    if (!(_page.SidebarFrame.Content is DMChannelsPage))
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
    }
}
