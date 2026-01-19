using System;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
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

        private void UsernameControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Unicord.Universal.Models.User.UserViewModel user)
            {
                Unicord.Universal.Utilities.AdaptiveFlyoutUtilities.ShowAdaptiveFlyout<Unicord.Universal.Controls.Flyouts.UserFlyout>(user, element);
            }
        }

        private void Avatar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel message && message.Author != null)
            {
                Unicord.Universal.Utilities.AdaptiveFlyoutUtilities.ShowAdaptiveFlyout<Unicord.Universal.Controls.Flyouts.UserFlyout>(message.Author, element, FlyoutPlacementMode.Right);
            }
        }
    }
}
