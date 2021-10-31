using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Pages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class MessageContextFlyout : Microsoft.UI.Xaml.Controls.CommandBarFlyout
    {
        public MessageControl Parent
        {
            get => (MessageControl)GetValue(ParentProperty);
            set => SetValue(ParentProperty, value);
        }

        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.Register("Parent", typeof(MessageControl), typeof(MessageContextFlyout), new PropertyMetadata(null));

        public MessageContextFlyout()
        {
            this.InitializeComponent();
        }

        // todo: is there a less shit way of doing this?
        private void EditFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.Target.FindParent<MessageControl>().BeginEdit();
        }

        // ditto
        private void AddReactionButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var control = this.Target.FindParent<MessageControl>();
            var page = this.Target.FindParent<ChannelPage>();

            page.ShowReactionPicker(control.MessageViewModel);
        }

        // HACK
        private void HideOnClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
