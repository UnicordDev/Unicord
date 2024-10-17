using System;
using System.Linq;
using Unicord.Universal.Controls.Flyouts;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Messages;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Resources.Controls
{
    public partial class Messages : ResourceDictionary
    {
        public Messages()
        {
            InitializeComponent();
        }

        public Uri ToUri(object obj) => (Uri)obj;

        private void ImageContainer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ImageBrush imageBrush = null;
            if (imageBrush == null)
            {
                var container = (Ellipse)sender;
                if (container == null || container.Fill == null)
                    return;

                imageBrush = (ImageBrush)container.Fill;
            }

            imageBrush.ImageSource = null;

            if (args.NewValue is not MessageViewModel message || message.Author == null || message.Author.AvatarUrl == null)
                return;

            imageBrush.ImageSource = new BitmapImage
            {
                UriSource = new Uri(message.Author.AvatarUrl),
                DecodePixelHeight = 36,
                DecodePixelWidth = 36,
                DecodePixelType = DecodePixelType.Logical
            };
        }

        private void Grid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var dc = ((FrameworkElement)sender).DataContext;
            var parent = sender.FindParent<Grid>("MessageControl_MainBorder");
            if (parent == null) return;

            // more of a guard, we're creating a new one anyway
            if (parent.ContextFlyout is not MessageContextFlyout flyout || parent.Tag == dc)
                return;

            parent.Tag = dc;

            var old = flyout.SecondaryCommands.OfType<FrameworkElement>().Where(c => (c.Tag as string) == "canary");
            foreach (var c in old)
                flyout.SecondaryCommands.Remove((ICommandBarElement)c);

            var separator = flyout.SecondaryCommands.LastOrDefault(c => c is AppBarSeparator);
            var index = flyout.SecondaryCommands.IndexOf(separator);

            var currentFlyout = (MenuFlyout)(((Grid)sender).Tag); // hate this
            foreach (var item in currentFlyout.Items.OfType<MenuFlyoutItem>().Reverse())
            {
                if (item == null) continue;

                var appbarItem = new AppBarButton() { Icon = item.Icon, Label = item.Text, Command = item.Command, Tag = "canary", DataContext = dc };
                flyout.SecondaryCommands.Insert(index, appbarItem);
            }

            flyout.SecondaryCommands.Insert(index, new AppBarSeparator() { Tag = "canary" });

            args.Handled = false;
        }
    }
}
