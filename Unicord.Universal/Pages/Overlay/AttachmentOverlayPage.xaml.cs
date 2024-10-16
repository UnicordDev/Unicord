using System;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
            overlayProgressRing.Visibility = Visibility.Visible;
            FailurePanel.Visibility = Visibility.Collapsed;

            if (e.Parameter is AttachmentViewModel attachment)
            {
                scaledControl.TargetWidth = attachment.NaturalWidth;
                scaledControl.TargetHeight = attachment.NaturalHeight;
                attachmentImage.MaxWidth = attachment.NaturalWidth;
                attachmentImage.MaxHeight = attachment.NaturalHeight;

                AttachmentSource.UriSource = new Uri(attachment.ProxyUrl);
            }

            if (e.Parameter is EmbedImageViewModel thumbnail)
            {
                scaledControl.TargetWidth = thumbnail.NaturalWidth;
                scaledControl.TargetHeight = thumbnail.NaturalHeight;
                attachmentImage.MaxWidth = thumbnail.NaturalWidth;
                attachmentImage.MaxHeight = thumbnail.NaturalHeight;

                AttachmentSource.UriSource = new Uri(thumbnail.Url);
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

        private void AttachmentSource_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            overlayProgressRing.Visibility = Visibility.Collapsed;
            FailurePanel.Visibility = Visibility.Visible;
        }

        private void attachmentImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}