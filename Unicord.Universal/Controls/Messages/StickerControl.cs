using System;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.UI.Xaml.Controls;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    public sealed class StickerControl : Control
    {
        private bool _templateApplied = false;

        public StickerViewModel Sticker
        {
            get => (StickerViewModel)GetValue(StickerProperty);
            set => SetValue(StickerProperty, value);
        }

        public static readonly DependencyProperty StickerProperty =
            DependencyProperty.Register("Sticker", typeof(StickerViewModel), typeof(StickerControl), new PropertyMetadata(null, OnStickerChanged));

        public StickerControl()
        {
            this.DefaultStyleKey = typeof(StickerControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _templateApplied = true;
        }

        private static void OnStickerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is StickerViewModel s && d is StickerControl c)
            {
                c.UpdateSticker(s);
            }
        }

        private void UpdateSticker(StickerViewModel s)
        {
            if (!_templateApplied)
                _templateApplied = this.ApplyTemplate();

            var root = (Border)this.GetTemplateChild("Root");
            switch (s.Format)
            {
                case StickerFormat.PNG:
                case StickerFormat.APNG:
                    root.Child = new ImageElement() { ImageUri = new Uri(s.StickerUrl), ImageWidth = 150, ImageHeight = 150, IsSpoiler = false };
                    break;
                case StickerFormat.LOTTIE:
                    {
                        var player = new AnimatedVisualPlayer
                        {
                            AutoPlay = true,
                            Width = 150,
                            Height = 150
                        };

                        var source = new LottieVisualSource();
                        source.UriSource = new Uri(s.StickerUrl);
                        player.Source = source;
                        root.Child = player;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
