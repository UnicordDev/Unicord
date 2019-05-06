using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Unicord.Universal.Themes
{
    public sealed partial class Templates : ResourceDictionary
    {
        public Templates()
        {
            InitializeComponent();
        }

        private void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if(args.SwipeControl.DataContext is DiscordChannel channel)
            {
                channel.Muted = !channel.Muted;
            }
        }
    }
}
