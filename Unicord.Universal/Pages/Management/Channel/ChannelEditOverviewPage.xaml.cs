using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus;
using Unicord.Universal.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Management.Channel
{
    public sealed partial class ChannelEditOverviewPage : Page
    {
        public ChannelEditOverviewPage()
        {
            this.InitializeComponent();
        }

        private void TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            var model = (DataContext as ChannelEditViewModel);
            if (model.Channel.Type == ChannelType.Text)
            {
                sender.Text = sender.Text.Replace(' ', '-').ToLowerInvariant();
                sender.Select(sender.Text.Length, 0);
            }
        }
    }
}
