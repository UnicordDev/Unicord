using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Services;
using Unicord.Universal.Converters;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Unicord.Universal.Pages.Overlay
{
    public sealed partial class AttachmentOverlayPage : Page, IOverlay
    {
        private bool _isZoomMode;
        private bool _isPanning;
        private Point _lastPointerPosition;
        private MessageViewModel _messageViewModel;
        private ulong _attachmentId;
        private Uri _currentAttachmentUri;
        private string _originalFileName;

        public AttachmentOverlayPage()
        {
            this.InitializeComponent();
            UpdateZoomButtonVisual();
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequested;
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

                AttachmentSource.UriSource = new Uri(attachment.ProxyUrl);
                _currentAttachmentUri = new Uri(attachment.ProxyUrl);
                
                _messageViewModel = attachment.Parent as MessageViewModel;
                // Access the attachment ID through reflection since it's not exposed as a public property
                var attachmentType = attachment.GetType();
                var attachmentField = attachmentType.GetField("_attachment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (attachmentField?.GetValue(attachment) is DSharpPlus.Entities.DiscordAttachment discordAttachment)
                {
                    _attachmentId = discordAttachment.Id;
                    _originalFileName = discordAttachment.FileName;
                }
                UpdateSenderInfo();
            }

            if (e.Parameter is EmbedImageViewModel thumbnail)
            {
                scaledControl.TargetWidth = thumbnail.NaturalWidth;
                scaledControl.TargetHeight = thumbnail.NaturalHeight;

                AttachmentSource.UriSource = new Uri(thumbnail.Url);
                _currentAttachmentUri = new Uri(thumbnail.Url);
                
                var embedViewModel = thumbnail.Parent as EmbedViewModel;
                _messageViewModel = embedViewModel?.Parent as MessageViewModel;
                _attachmentId = 0; // Embed images don't have attachment IDs
                UpdateSenderInfo();
            }

            // Always monitor view changes so pinch/scroll can dynamically toggle zoom mode
            imageScrollViewer.ViewChanged -= ImageScrollViewer_ViewChanged;
            imageScrollViewer.ViewChanged += ImageScrollViewer_ViewChanged;
            
            // Initialize zoom combobox
            UpdateZoomComboBox();
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

        private void attachmentImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;

            // If currently not in zoom mode, zoom into the tapped point.
            if (!_isZoomMode)
            {
                // Compute the same target zoom used by the toggle (cover * 1.5)
                var w = scaledControl.TargetWidth;
                var h = scaledControl.TargetHeight;
                var bounds = Window.Current.Bounds;
                var winW = bounds.Width;
                var winH = bounds.Height;

                var mw_n = winW - 80;
                var mh_n = winH - 160;
                var s_c = Math.Min(1.0, Math.Min(mw_n / w, mh_n / h));
                var s_v = Math.Max(winW / w, winH / h);
                var targetZoom = (float)((s_v / s_c) * 1.5);

                // Get pointer position relative to the scaled content
                var pointer = e.GetPosition(scaledControl);

                // Calculate target offsets so the pointer stays centered after zoom
                var targetWidth = scaledControl.ActualWidth * targetZoom;
                var targetHeight = scaledControl.ActualHeight * targetZoom;
                var centerX = Math.Max(0, pointer.X * targetZoom - imageScrollViewer.ViewportWidth / 2);
                var centerY = Math.Max(0, pointer.Y * targetZoom - imageScrollViewer.ViewportHeight / 2);

                // Clamp to scrollable area
                centerX = Math.Min(centerX, Math.Max(0, targetWidth - imageScrollViewer.ViewportWidth));
                centerY = Math.Min(centerY, Math.Max(0, targetHeight - imageScrollViewer.ViewportHeight));

                // Enter zoom mode state
                _isZoomMode = true;
                UpdateZoomButtonVisual();
                background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));

                // Ensure we monitor view changes and enable zoom
                EnsureZoomEnabled();

                imageScrollViewer.ChangeView(centerX, centerY, targetZoom, false);
            }
            else
            {
                // Exit zoom mode
                _isZoomMode = false;
                UpdateZoomButtonVisual();

                // Animate back to fit
                imageScrollViewer.ChangeView(0, 0, 1.0f, false);
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            var current = imageScrollViewer.ZoomFactor;
            var step = 1.25f;
            var target = Math.Min(imageScrollViewer.MaxZoomFactor, current * step);

            // Calculate viewport center in content coordinates
            var viewportW = imageScrollViewer.ViewportWidth;
            var viewportH = imageScrollViewer.ViewportHeight;
            var contentCenterX = imageScrollViewer.HorizontalOffset + viewportW / 2.0;
            var contentCenterY = imageScrollViewer.VerticalOffset + viewportH / 2.0;

            var ratio = target / current;

            // Map center to new offset so center remains centered
            var newCenterX = contentCenterX * ratio;
            var newCenterY = contentCenterY * ratio;
            var targetOffsetX = Math.Max(0, newCenterX - viewportW / 2.0);
            var targetOffsetY = Math.Max(0, newCenterY - viewportH / 2.0);

            // Clamp to max scrollable size
            var contentWidth = scaledControl.ActualWidth * target;
            var contentHeight = scaledControl.ActualHeight * target;
            var maxOffsetX = Math.Max(0, contentWidth - viewportW);
            var maxOffsetY = Math.Max(0, contentHeight - viewportH);
            targetOffsetX = Math.Min(targetOffsetX, maxOffsetX);
            targetOffsetY = Math.Min(targetOffsetY, maxOffsetY);

            // If not in zoom mode, enter it
            if (!_isZoomMode)
            {
                _isZoomMode = true;
                UpdateZoomButtonVisual();
                background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));
                EnsureZoomEnabled();
            }

            imageScrollViewer.ChangeView(targetOffsetX, targetOffsetY, target, false);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var current = imageScrollViewer.ZoomFactor;
            var step = 1.25f;
            var target = Math.Max(1.0f, current / step);

            // Calculate viewport center in content coordinates
            var viewportW = imageScrollViewer.ViewportWidth;
            var viewportH = imageScrollViewer.ViewportHeight;
            var contentCenterX = imageScrollViewer.HorizontalOffset + viewportW / 2.0;
            var contentCenterY = imageScrollViewer.VerticalOffset + viewportH / 2.0;

            var ratio = target / current;
            var newCenterX = contentCenterX * ratio;
            var newCenterY = contentCenterY * ratio;
            var targetOffsetX = Math.Max(0, newCenterX - viewportW / 2.0);
            var targetOffsetY = Math.Max(0, newCenterY - viewportH / 2.0);

            var contentWidth = scaledControl.ActualWidth * target;
            var contentHeight = scaledControl.ActualHeight * target;
            var maxOffsetX = Math.Max(0, contentWidth - viewportW);
            var maxOffsetY = Math.Max(0, contentHeight - viewportH);
            targetOffsetX = Math.Min(targetOffsetX, maxOffsetX);
            targetOffsetY = Math.Min(targetOffsetY, maxOffsetY);

            imageScrollViewer.ChangeView(targetOffsetX, targetOffsetY, target, false);
        }

        private void imageScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(imageScrollViewer).Properties;
            // allow panning when user has zoomed in or when zoom-mode was explicitly enabled
            if (properties.IsLeftButtonPressed && (imageScrollViewer.ZoomFactor > 1.0 || _isZoomMode))
            {
                _isPanning = true;
                _lastPointerPosition = e.GetCurrentPoint(imageScrollViewer).Position;
                imageScrollViewer.CapturePointer(e.Pointer);
            }
        }

        private void imageScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(imageScrollViewer).Properties;
            
            // Stop panning if left button is no longer pressed
            if (_isPanning && !properties.IsLeftButtonPressed)
            {
                _isPanning = false;
                return;
            }
            
            if (_isPanning)
            {
                var currentPosition = e.GetCurrentPoint(imageScrollViewer).Position;
                var deltaX = currentPosition.X - _lastPointerPosition.X;
                var deltaY = currentPosition.Y - _lastPointerPosition.Y;

                imageScrollViewer.ChangeView(imageScrollViewer.HorizontalOffset - deltaX, imageScrollViewer.VerticalOffset - deltaY, null, true);
                _lastPointerPosition = currentPosition;
            }
        }

        private void imageScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                imageScrollViewer.ReleasePointerCapture(e.Pointer);
            }
        }

        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = AttachmentSource?.UriSource;
                if (uri != null)
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
            catch { /* ignore failures silently */ }
        }

        private async void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = AttachmentSource?.UriSource;
                if (uri != null)
                {
                    var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                    savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                    
                    // Detect file extension from original filename
                    string extension = ".png";
                    string fileNameWithoutExt = "attachment";
                    
                    if (!string.IsNullOrEmpty(_originalFileName))
                    {
                        extension = System.IO.Path.GetExtension(_originalFileName).ToLowerInvariant();
                        fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(_originalFileName);
                        
                        if (string.IsNullOrEmpty(extension))
                            extension = ".png";
                    }
                    
                    // Map extension to file type choice
                    var fileTypeName = extension.TrimStart('.').ToUpperInvariant() + " Image";
                    savePicker.FileTypeChoices.Add(fileTypeName, new List<string> { extension });
                    
                    // Add other common image formats as alternatives
                    if (extension != ".png")
                        savePicker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
                    if (extension != ".jpg" && extension != ".jpeg")
                        savePicker.FileTypeChoices.Add("JPEG Image", new List<string> { ".jpg", ".jpeg" });
                    if (extension != ".gif")
                        savePicker.FileTypeChoices.Add("GIF Image", new List<string> { ".gif" });
                    if (extension != ".webp")
                        savePicker.FileTypeChoices.Add("WebP Image", new List<string> { ".webp" });
                    
                    savePicker.SuggestedFileName = fileNameWithoutExt;

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        try
                        {
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                var bytes = await httpClient.GetByteArrayAsync(uri);
                                await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);
                            }
                            
                            // Show success banner
                            ShowSuccessBanner();
                        }
                        catch { /* ignore download errors */ }
                    }
                }
            }
            catch { /* ignore failures silently */ }
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTransferManager.ShowShareUI();
            }
            catch { /* ignore failures silently */ }
        }

        private async void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (_currentAttachmentUri != null)
            {
                var deferral = args.Request.GetDeferral();
                try
                {
                    var request = args.Request;
                    
                    // Download the image to a temporary file
                    var tempFolder = ApplicationData.Current.TemporaryFolder;
                    var fileName = Path.GetFileName(_currentAttachmentUri.LocalPath);
                    if (string.IsNullOrEmpty(fileName) || fileName == "/")
                    {
                        fileName = "image.png";
                    }
                    
                    var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    
                    using (var client = new HttpClient())
                    {
                        var imageData = await client.GetByteArrayAsync(_currentAttachmentUri);
                        await FileIO.WriteBytesAsync(tempFile, imageData);
                    }
                    
                    // Share the file with thumbnail
                    var storageItems = new List<IStorageItem> { tempFile };
                    request.Data.SetStorageItems(storageItems);
                    
                    // Set thumbnail for preview
                    var thumbnail = RandomAccessStreamReference.CreateFromFile(tempFile);
                    request.Data.Properties.Thumbnail = thumbnail;
                    
                    request.Data.Properties.Title = fileName;
                    request.Data.Properties.Description = "Image from Unicord";
                }
                catch { /* ignore share data request errors */ }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = AttachmentSource?.UriSource;
                if (uri != null)
                {
                    var dp = new DataPackage();
                    dp.SetText(uri.ToString());
                    Clipboard.SetContent(dp);
                }
            }
            catch { /* ignore failures silently */ }
        }

        private void CopyImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = AttachmentSource?.UriSource;
                if (uri != null)
                {
                    var dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    var ras = RandomAccessStreamReference.CreateFromUri(uri);
                    dp.SetBitmap(ras);
                    Clipboard.SetContent(dp);
                }
            }
            catch { /* ignore failures silently */ }
        }

        private void CopyAttachmentId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_attachmentId != 0)
                {
                    var dp = new DataPackage();
                    dp.SetText(_attachmentId.ToString());
                    Clipboard.SetContent(dp);
                }
            }
            catch { /* ignore failures silently */ }
        }

        private void ImageScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // Update zoom combobox to show current zoom level
            UpdateZoomComboBox();

            // Ignore intermediate (in-progress) changes; act only when finalized
            if (e.IsIntermediate)
                return;

            // If user manually zooms (pinch/ctrl+wheel) beyond 1.0 while toggle is off, enter zoom mode
            if (!_isZoomMode && imageScrollViewer.ZoomFactor > 1.01)
            {
                _isZoomMode = true;
                UpdateZoomButtonVisual();
                EnsureZoomEnabled();
                background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));
                return;
            }

            // If user manually zooms back to normal while in zoom mode, exit zoom mode (animate to center)
            if (_isZoomMode && imageScrollViewer.ZoomFactor <= 1.01)
            {
                _isZoomMode = false;
                UpdateZoomButtonVisual();

                // Animate back to centered normal view; after animation completes this handler will be invoked again (final) and clean up
                imageScrollViewer.ChangeView(0, 0, 1.0f, false);
                return;
            }

            // When exiting zoom mode programmatically we animate back to 1.0f; once reached, clean up visuals
            if (!_isZoomMode && Math.Abs(imageScrollViewer.ZoomFactor - 1.0) < 0.01)
            {
                background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));
            }
        }

        private void ZoomModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isZoomMode)
                EnterZoomMode();
            else
                ExitZoomMode();
        }

        private void EnsureZoomEnabled()
        {
            imageScrollViewer.ViewChanged -= ImageScrollViewer_ViewChanged;
            imageScrollViewer.ViewChanged += ImageScrollViewer_ViewChanged;
            imageScrollViewer.MaxZoomFactor = 10;
            imageScrollViewer.ZoomMode = ZoomMode.Enabled;
            imageScrollViewer.HorizontalScrollMode = ScrollMode.Auto;
            imageScrollViewer.VerticalScrollMode = ScrollMode.Auto;
        }

        private void UpdateZoomButtonVisual()
        {
            if (zoomFrontIcon != null)
                zoomFrontIcon.Visibility = _isZoomMode ? Visibility.Collapsed : Visibility.Visible;
            if (zoomModeButton != null)
            {
                var tip = _isZoomMode ? "Zoom to fit" : "Zoom to actual size";
                ToolTipService.SetToolTip(zoomModeButton, tip);
            }
        }

        private void EnterZoomMode()
        {
            _isZoomMode = true;
            UpdateZoomButtonVisual();
            background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));
            EnsureZoomEnabled();

            var w = scaledControl.TargetWidth;
            var h = scaledControl.TargetHeight;
            var bounds = Window.Current.Bounds;
            var winW = bounds.Width;
            var winH = bounds.Height;

            var mw_n = winW - 80;
            var mh_n = winH - 160;
            var s_c = Math.Min(1.0, Math.Min(mw_n / w, mh_n / h));
            var s_v = Math.Max(winW / w, winH / h);
            var targetZoom = (float)((s_v / s_c) * 1.5);

            var targetWidth = scaledControl.ActualWidth * targetZoom;
            var targetHeight = scaledControl.ActualHeight * targetZoom;
            var centerX = Math.Max(0, (targetWidth - imageScrollViewer.ViewportWidth) / 2);
            var centerY = Math.Max(0, (targetHeight - imageScrollViewer.ViewportHeight) / 2);

            imageScrollViewer.ChangeView(centerX, centerY, targetZoom, false);
        }

        private void ExitZoomMode()
        {
            _isPanning = false;
            _isZoomMode = false;
            UpdateZoomButtonVisual();

            imageScrollViewer.ViewChanged -= ImageScrollViewer_ViewChanged;
            imageScrollViewer.ViewChanged += ImageScrollViewer_ViewChanged;
            imageScrollViewer.ChangeView(0, 0, 1.0f, false);
        }

        private void UpdateSenderInfo()
        {
            if (_messageViewModel != null && senderNameText != null && timestampText != null)
            {
                senderNameText.Text = _messageViewModel.Author?.DisplayName ?? "Unknown";
                
                var timestampStyle = (TimestampStyle)App.RoamingSettings.Read(Constants.TIMESTAMP_STYLE, (int)TimestampStyle.Absolute);
                timestampText.Text = DateTimeConverter.Convert(timestampStyle, _messageViewModel.Timestamp.DateTime);
                
                if (avatarBrush != null && _messageViewModel.Author?.AvatarUrl != null)
                {
                    avatarBrush.ImageSource = new BitmapImage(new Uri(_messageViewModel.Author.AvatarUrl));
                }
            }
        }

        private void UpdateZoomComboBox()
        {
            if (zoomComboBox == null) return;

            var currentZoom = imageScrollViewer.ZoomFactor;
            var percentText = $"{Math.Round(currentZoom * 100)}%";
            
            // Simply update the text display without triggering events
            zoomComboBox.SelectionChanged -= ZoomComboBox_SelectionChanged;
            zoomComboBox.TextSubmitted -= ZoomComboBox_TextSubmitted;
            
            zoomComboBox.Text = percentText;
            
            zoomComboBox.SelectionChanged += ZoomComboBox_SelectionChanged;
            zoomComboBox.TextSubmitted += ZoomComboBox_TextSubmitted;
        }

        private void ZoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (zoomComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
            {
                if (float.TryParse(tagStr, out var zoomLevel))
                {
                    SetZoomLevel(zoomLevel);
                }
            }
        }

        private void ZoomComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            ApplyZoomFromText(args.Text);
        }

        private void ZoomComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Update display to show actual current zoom
            UpdateZoomComboBox();
        }

        private void ZoomComboBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ApplyZoomFromText(zoomComboBox.Text);
                e.Handled = true;
            }
        }

        private void ApplyZoomFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                UpdateZoomComboBox();
                return;
            }

            text = text.Trim().TrimEnd('%');
            if (float.TryParse(text, out var percent) && percent >= 100 && percent <= 1000)
            {
                var zoomLevel = percent / 100f;
                SetZoomLevel(zoomLevel);
            }
            else
            {
                UpdateZoomComboBox(); // Reset to current value if invalid
            }
        }

        private void SetZoomLevel(float zoomLevel)
        {
            // Clamp to valid range
            zoomLevel = Math.Clamp(zoomLevel, 0.1f, imageScrollViewer.MaxZoomFactor);

            if (zoomLevel <= 1.0f)
            {
                // Zoom to fit - exit zoom mode
                if (_isZoomMode)
                {
                    ExitZoomMode();
                }
                else
                {
                    imageScrollViewer.ChangeView(0, 0, 1.0f, false);
                }
            }
            else
            {
                // Zoom in - enter zoom mode if not already
                if (!_isZoomMode)
                {
                    _isZoomMode = true;
                    UpdateZoomButtonVisual();
                    background.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x00, 0x00, 0x00));
                    EnsureZoomEnabled();
                }
                
                // Zoom to center of image content (not viewport center)
                var viewportW = imageScrollViewer.ViewportWidth;
                var viewportH = imageScrollViewer.ViewportHeight;
                
                // Calculate image center position at target zoom
                var contentWidth = scaledControl.ActualWidth * zoomLevel;
                var contentHeight = scaledControl.ActualHeight * zoomLevel;
                
                // Center the image in the viewport
                var targetOffsetX = Math.Max(0, (contentWidth - viewportW) / 2.0);
                var targetOffsetY = Math.Max(0, (contentHeight - viewportH) / 2.0);
                
                imageScrollViewer.ChangeView(targetOffsetX, targetOffsetY, zoomLevel, false);
            }
        }

        private async void ShowSuccessBanner()
        {
            if (successInfoBar == null) return;

            var resourceLoader = ResourceLoader.GetForCurrentView();
            successInfoBar.Message = resourceLoader.GetString("AttachmentOverlay_SaveSuccessMessage");
            successInfoBar.IsOpen = true;
            
            // Auto-hide after 3 seconds
            await System.Threading.Tasks.Task.Delay(3000);
            successInfoBar.IsOpen = false;
        }
    }
}