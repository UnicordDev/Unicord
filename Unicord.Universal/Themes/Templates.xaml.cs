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
using System.ComponentModel;

namespace Unicord.Universal.Resources
{
    public sealed partial class Templates : ResourceDictionary
    {
        public Templates()
        {
            InitializeComponent();
        }

        private void OnGuildsListFolderItemLoaded(object sender, RoutedEventArgs e)
        {
            // hack for RS2 
            var parent = ((Grid)sender).FindParent<MUXC.TreeViewItem>();
            if (parent == null) return;

            var vm = (GuildListFolderViewModel)(((Grid)sender).DataContext);
            if (vm == null) return;

            long token = -1;
            void DependencyPropertyChangedCallback(DependencyObject sender, DependencyProperty e)
            {
                vm.IsExpanded = parent.IsExpanded;
            }

            void ViewModelPropertyChangedCallback(object o, PropertyChangedEventArgs ev)
            {
                if (ev.PropertyName == nameof(vm.IsExpanded))
                    parent.IsExpanded = vm.IsExpanded;
            }

            void Unloaded(object sender, RoutedEventArgs e)
            {
                vm.PropertyChanged -= ViewModelPropertyChangedCallback;
                if (token != -1)
                    parent.UnregisterPropertyChangedCallback(MUXC.TreeViewItem.IsExpandedProperty, token);
                parent.Unloaded -= Unloaded;
            }

            parent.ItemsSource = vm.Children;
            parent.IsExpanded = vm.IsExpanded;

            parent.Unloaded += Unloaded;
            vm.PropertyChanged += ViewModelPropertyChangedCallback;
            token = parent.RegisterPropertyChangedCallback(MUXC.TreeViewItem.IsExpandedProperty, DependencyPropertyChangedCallback);
        }

        private void OnGuildsListItemLoaded(object sender, RoutedEventArgs e)
        {
            // hack for RS2 
            var parent = ((Grid)sender).FindParent<MUXC.TreeViewItem>();
            if (parent == null) return;

            var vm = (GuildListViewModel)(((Grid)sender).DataContext);
            if (vm == null) return;

            long token = -1;
            void DependencyPropertyChangedCallback(DependencyObject sender, DependencyProperty e)
            {
                vm.IsSelected = parent.IsSelected;
            }

            void ViewModelPropertyChangedCallback(object o, PropertyChangedEventArgs ev)
            {
                if (ev.PropertyName == nameof(vm.IsSelected))
                    parent.IsSelected = vm.IsSelected;
            }

            void Unloaded(object sender, RoutedEventArgs e)
            {
                vm.PropertyChanged -= ViewModelPropertyChangedCallback;
                if (token != -1)
                    parent.UnregisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, token);
                parent.Unloaded -= Unloaded;
            }

            parent.IsSelected = vm.IsSelected;

            parent.Unloaded += Unloaded;
            vm.PropertyChanged += ViewModelPropertyChangedCallback;
            token = parent.RegisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, DependencyPropertyChangedCallback);

        }
    }
}
