using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Overlay
{
    public sealed partial class AttachmentOverlayPage : Page, IOverlay
    {
        public AttachmentOverlayPage()
        {
            this.InitializeComponent();
        }

        public Size PreferredSize { get; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            contentContainerOverlay.Visibility = Visibility.Visible;

            if (e.Parameter is DiscordAttachment attachment)
            {
                scaledControl.TargetWidth = attachment.Width;
                scaledControl.TargetHeight = attachment.Height;
                attachmentImage.MaxWidth = attachment.Width;
                attachmentImage.MaxHeight = attachment.Height;

                AttachmentSource.UriSource = new Uri(attachment.ProxyUrl);
            }

            if (e.Parameter is DiscordEmbedThumbnail thumbnail)
            {
                scaledControl.TargetWidth = thumbnail.Width;
                scaledControl.TargetHeight = thumbnail.Height;
                attachmentImage.MaxWidth = thumbnail.Width;
                attachmentImage.MaxHeight = thumbnail.Height;

                AttachmentSource.UriSource = thumbnail.ProxyUrl.ToUri();
            }

            if (e.Parameter is DiscordEmbedImage image)
            {
                scaledControl.TargetWidth = image.Width;
                scaledControl.TargetHeight = image.Height;
                attachmentImage.MaxWidth = image.Width;
                attachmentImage.MaxHeight = image.Height;

                AttachmentSource.UriSource = image.ProxyUrl.ToUri();
            }
        }

        private void AttachmentSource_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            overlayProgressRing.Value = e.Progress;
        }

        private void AttachmentSource_ImageOpened(object sender, RoutedEventArgs e)
        {
            contentContainerOverlay.Visibility = Visibility.Collapsed;
        }

        private void contentContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            OverlayService.GetForCurrentView().CloseOverlay();
        }
    }
}
