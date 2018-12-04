using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Controls.Embed;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls
{
    public sealed partial class EmbedViewer : UserControl
    {
        private DiscordEmbed _embed;
        private DiscordChannel _channel;
        private bool _showThumbnail;

        public EmbedViewer()
        {
            InitializeComponent();
        }

        public EmbedViewer(DiscordMessage m, DiscordEmbed embed)
        {
            _embed = embed;
            _channel = m.Channel;
            InitializeComponent();
            thumbnail.Visibility = Visibility.Visible;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_embed.Type == "image" && _embed.Image != null)
            {
                Content = new ImageElement() { ImageUri = _embed.Image.ProxyUrl, ImageWidth = _embed.Image.Width, ImageHeight = _embed.Image.Height };
                return;
            }

            if (_embed.Image != null)
            {
                var image = new ImageElement() { ImageUri = _embed.Image.ProxyUrl, ImageWidth = _embed.Image.Width, ImageHeight = _embed.Image.Height };
                mainGrid.Children.Add(image);
            }

            if (_embed.Thumbnail != null)
            {
                thumbnail.Visibility = Visibility.Visible;
            }

            if (_embed.Video != null)
            {
                thumbnail.Visibility = Visibility.Collapsed;
                var video = new EmbedVideoControl() { Thumbnail = _embed.Thumbnail, Video = _embed.Video };
                mainGrid.Children.Add(video);
            }

            if (_embed.Color.HasValue)
            {
                var col = _embed.Color.Value;
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));
            }

            if (_embed.Fields?.Any() == true)
            {
                var inline = _embed.Fields.First().Inline;
                var p = inline ? new WrapPanel() : (Panel)new StackPanel();
                foreach (var field in _embed.Fields)
                {
                    if (field.Inline == inline)
                    {
                        AddFieldToPanel(p, field);
                    }
                    else
                    {
                        if (p.Children.Count != 0)
                        {
                            AddWithRow(p);
                        }

                        inline = field.Inline;

                        p = inline ? new WrapPanel() : (Panel)new StackPanel();

                        AddFieldToPanel(p, field);
                    }
                }

                if (p.Children.Count != 0 && !fieldsGrid.Children.Contains(p))
                {
                    AddWithRow(p);
                }
            }

            Bindings.Update();
        }

        private void AddWithRow(FrameworkElement p)
        {
            fieldsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(p, fieldsGrid.RowDefinitions.Count - 1);
            fieldsGrid.Children.Add(p);
        }

        private void AddFieldToPanel(Panel p, DiscordEmbedField field)
        {
            p.Children.Add(new EmbedFieldControl(_channel, field));
        }
    }
}
