using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class SearchPage : Page
    {
        private ChannelViewModel _channel;
        public SearchPageViewModel ViewModel { get; set; }

        public SearchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ChannelViewModel channel)
            {
                _channel = channel;
                ViewModel = new SearchPageViewModel(channel);
                Root.DataContext = ViewModel;
            }
            else
            {
                _channel = null;
                ViewModel = null;
                Root.DataContext = null;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentPage = 1;
            await ViewModel.SearchAsync(SearchBox.Text);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentPage++;
            await ViewModel.SearchAsync(SearchBox.Text);
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentPage--;
            await ViewModel.SearchAsync(SearchBox.Text);
        }

        private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                ViewModel.CurrentPage = 1;
                await ViewModel.SearchAsync(SearchBox.Text);
            }
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            SplitPaneService.GetForCurrentView().ToggleRightPane<SearchPage>(_channel);
        }
    }
}
