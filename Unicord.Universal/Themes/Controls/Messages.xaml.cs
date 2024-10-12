using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messages;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    }
}
