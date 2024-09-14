using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;
using MUXC = Microsoft.UI.Xaml.Controls;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Resources
{
    public sealed partial class Templates : ResourceDictionary
    {
        public Templates()
        {
            InitializeComponent();
        }

        private void OnGuildsListFolderLoaded(object sender, RoutedEventArgs e)
        {
            // hack for RS2 
            var parent = ((Grid)sender).FindParent<MUXC.TreeViewItem>();
            var vm = (GuildListFolderViewModel)(((Grid)sender).DataContext);
            parent.ItemsSource = vm.Children;
            parent.IsExpanded = vm.IsExpanded;
            vm.PropertyChanged += (o, ev) =>
            {
                if (ev.PropertyName == "IsExpanded")
                    parent.IsExpanded = vm.IsExpanded;
            };
        }
    }
}
