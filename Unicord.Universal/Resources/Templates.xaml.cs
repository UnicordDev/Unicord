using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls.Primitives;

namespace Unicord.Universal.Resources
{
    public sealed partial class Templates : ResourceDictionary
    {
        public Templates()
        {
            InitializeComponent();
        }

        private void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is DiscordChannel channel)
            {
                channel.Muted = !channel.Muted;
            }
        }

        private void OnDeferredContextFlyoutRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            (element.FindName("ContextFlyout") as FlyoutBase)?.ShowAt(element);
        }
    }
}
