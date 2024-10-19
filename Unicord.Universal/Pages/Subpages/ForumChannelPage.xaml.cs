using System;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Unicord.Universal.Behaviours;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Subpages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ForumChannelPage : Page
    {
        public ForumChannelViewModel ViewModel
        {
            get { return (ForumChannelViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ForumChannelViewModel), typeof(ForumChannelPage), new PropertyMetadata(null));

        public ForumChannelPage()
        {
            this.InitializeComponent();


#if !(ARM && DEBUG)
            ListViewBehaviour.SetMinItemWidth(this.ChannelsGrid, 400);
            ItemsReorderAnimation.SetDuration(this.ChannelsGrid, TimeSpan.FromMilliseconds(250));
#endif
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is not DiscordForumChannel forum)
                return;

            ViewModel = new ForumChannelViewModel(forum.Id);
            await ViewModel.LoadPostData();
        }

        private async void ChannelsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ForumThreadViewModel vm)
            {
                await DiscordNavigationService.GetForCurrentView()
                    .NavigateAsync(vm.Channel);
            }
        }
    }
}
