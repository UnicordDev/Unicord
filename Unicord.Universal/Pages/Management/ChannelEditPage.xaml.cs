using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Management
{
    public sealed partial class ChannelEditPage : Page
    {
        private ChannelEditViewModel _viewModel;

        public ChannelEditPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordChannel channel)
            {
                // TODO: check perms
                _viewModel = new ChannelEditViewModel(channel);
                DataContext = _viewModel;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowManager.HandleTitleBarForGrid(topGrid);
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            acceptButton.Visibility = Visibility.Collapsed;
            mainContent.IsEnabled = false;
            backButton.IsEnabled = false;
            progressRing.IsActive = true;

            await _viewModel.SaveChangesAsync();

            progressRing.IsActive = false;
            this.FindParent<DiscordPage>().CloseCustomPane();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<DiscordPage>().CloseCustomPane();
        }

        private void TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (sender.Text.Contains(" "))
            {
                sender.Text = sender.Text.Replace(' ', '-');
                sender.Select(sender.Text.Length, 0);
            }
        }
    }
}
