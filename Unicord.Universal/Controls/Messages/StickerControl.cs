using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    public sealed class StickerControl : Control
    {
        private bool _templateApplied = false;

        public DiscordSticker Sticker
        {
            get => (DiscordSticker)GetValue(StickerProperty);
            set => SetValue(StickerProperty, value);
        }

        public static readonly DependencyProperty StickerProperty =
            DependencyProperty.Register("Sticker", typeof(DiscordSticker), typeof(StickerControl), new PropertyMetadata(null, OnStickerChanged));

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
            if (e.NewValue is DiscordSticker s && d is StickerControl c)
            {
                c.UpdateSticker(s);
            }
        }

        private void UpdateSticker(DiscordSticker s)
        {
            if (!_templateApplied)
                _templateApplied = this.ApplyTemplate();

            var root = (Border)this.GetTemplateChild("Root");
            switch (s.StickerType)
            {
                case DiscordStickerType.PNG:
                case DiscordStickerType.APNG:
                    root.Child = new ImageElement() { ImageUri = new Uri(s.GetAssetUrl()), ImageWidth = 150, ImageHeight = 150, IsSpoiler = false };
                    break;
                case DiscordStickerType.Lottie:
                    {
                        var player = new AnimatedVisualPlayer
                        {
                            AutoPlay = true,
                            Width = 150,
                            Height = 150
                        };

                        var source = new LottieVisualSource();
                        source.UriSource = new Uri(s.GetAssetUrl());
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
