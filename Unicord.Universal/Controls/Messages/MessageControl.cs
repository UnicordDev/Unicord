using System;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Controls.Messages
{
    public class MessageControl : Control
    {
        private ImageBrush _imageBrush;

        #region Dependency Properties

        public MessageViewModel MessageViewModel
        {
            get { return (MessageViewModel)GetValue(MessageViewModelProperty); }
            set { SetValue(MessageViewModelProperty, value); }
        }

        public static readonly DependencyProperty MessageViewModelProperty =
            DependencyProperty.Register("MessageViewModel", typeof(MessageViewModel), typeof(MessageControl), new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageControl control && e.Property == MessageViewModelProperty)
            {
                control.OnMessageChanged(e);
            }
        }

        #endregion

        public MessageControl()
        {
            this.DefaultStyleKey = typeof(MessageControl);
        }

        protected override void OnApplyTemplate()
        {
            //this.UpdateCollapsedState();
        }

        protected virtual void OnMessageChanged(DependencyPropertyChangedEventArgs e)
        {
            this.ApplyTemplate();

            if (e.NewValue is MessageViewModel message)
            {
                this.UpdateProfileImage(message);
            }
            else
            {
                this.ClearProfileImage();
            }
        }

        private void ClearProfileImage()
        {
            if (_imageBrush == null)
            {
                var container = (Ellipse)this.GetTemplateChild("ImageContainer");
                if (container == null || container.Fill == null)
                    return;

                _imageBrush = (ImageBrush)container.Fill;
            }

            _imageBrush.ImageSource = null;
        }

        private void UpdateProfileImage(MessageViewModel message)
        {
            this.ClearProfileImage();

            if (_imageBrush == null || message.Author == null || message.Author.AvatarUrl == null)
                return;

            _imageBrush.ImageSource = new BitmapImage
            {
                UriSource = new Uri(message.Author.AvatarUrl),
                DecodePixelHeight = 64,
                DecodePixelWidth = 64,
                DecodePixelType = DecodePixelType.Physical
            };
        }
    }
}
