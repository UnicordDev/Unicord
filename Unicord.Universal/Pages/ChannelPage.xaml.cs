using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Controls;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class ChannelPage : Page, INotifyPropertyChanged
    {
        private static ThreadLocal<ConcurrentDictionary<ulong, ChannelViewModel>> _channelHistory
            = new ThreadLocal<ConcurrentDictionary<ulong, ChannelViewModel>>(() => new ConcurrentDictionary<ulong, ChannelViewModel>());

        public ChannelViewModel ViewModel
        {
            get => _viewModel; private set
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }

        public bool IsPaneOpen { get; private set; }

        private EmotePicker _emotePicker;
        private ChannelViewModel _viewModel;
        private bool _scrollHandlerAdded;
        private VideoFrame _videoFrame;
        private string _error;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChannelPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            messageTextBox.KeyDown += messageTextBox_KeyDown;

            if (ApiInformation.IsTypePresent("Windows.UI.Core.SystemNavigationManager"))
            {
                var navigation = SystemNavigationManager.GetForCurrentView();
                navigation.BackRequested += Navigation_BackRequested;
            }

            if (App.StatusBarFill != default)
            {
                topGrid.Padding = App.StatusBarFill;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordChannel chan)
            {
                ChannelViewModel model = null;
                App._currentChannelId = chan.Id;

                if (_channelHistory.Value.TryGetValue(chan.Id, out var result))
                {
                    model = result;
                }

                if (ViewModel != null)
                {
                    _channelHistory.Value[ViewModel.Channel.Id] = ViewModel;
                }

                if (model == null)
                    model = new ChannelViewModel(chan);

                ViewModel = model;
                DataContext = ViewModel;

                while (_channelHistory.Value.Count > 10)
                {
                    var oldest = _channelHistory.Value.OrderBy(m => m.Value.LastAccessed.ToUnixTimeMilliseconds()).FirstOrDefault();
                    _channelHistory.Value.TryRemove(oldest.Key, out var value);

                    Logger.Log($"Removing ChannelViewModel for {oldest.Value.Channel}");

                    value.Dispose();
                }

                await Load();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_scrollHandlerAdded)
            {
                var scrollViewer = messageList.FindChild<ScrollViewer>("ScrollViewer");
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

                showSidebarButtonContainer.Visibility = this.FindParent<DiscordPage>() == null ? Visibility.Collapsed : Visibility.Visible;

                _scrollHandlerAdded = true;
            }
        }

        private async void Navigation_BackRequested(object sender, BackRequestedEventArgs e)
        {
            this.FindParent<MainPage>()?.LeaveFullscreen();

            var lastChannel = _channelHistory.Value.OrderBy(m => m.Value.LastAccessed).FirstOrDefault();
            if (lastChannel.Key != default)
            {
                e.Handled = true;

                ViewModel = lastChannel.Value;
                DataContext = ViewModel;

                await Load();
            }
        }

        private async Task Load()
        {
            ViewModel.LastAccessed = DateTimeOffset.Now;

            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    loadingProgress.Visibility = Visibility.Visible;
                    loadingProgress.IsIndeterminate = true;

                    noMessages.Visibility = Visibility.Collapsed;
                });

                await ViewModel.LoadMessagesAsync().ConfigureAwait(false);

                if (ViewModel.Channel.Guild?.IsSynced == false)
                {
                    await ViewModel.Channel.Guild.SyncAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                HockeyClient.Current.TrackException(ex);
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                loadingProgress.Visibility = Visibility.Collapsed;
                loadingProgress.IsIndeterminate = false;
            });

            if (IsPaneOpen)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    sidebarFrame.Navigate(sidebarFrame.CurrentSourcePageType, ViewModel.Channel));
            }

            await AddToJumpListAsync();
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scroll = sender as ScrollViewer;
            if (!e.IsIntermediate)
            {
                if (scroll.VerticalOffset >= (scroll.ScrollableHeight - scroll.ViewportHeight) && ViewModel.Channel.ReadState?.Unread != false)
                {
                    var message = messageList.Items.LastOrDefault() as DiscordMessage;
                    if (message != null)
                    {
                        await message.AcknowledgeAsync().ConfigureAwait(false);
                    }
                }
                else if (scroll.VerticalOffset <= 150)
                {
                    //_loading = true;

                    await ViewModel.LoadMessagesBeforeAsync();

                    //_loading = false;
                }
            }
        }

        private async void messageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter && shift.HasFlag(CoreVirtualKeyStates.None))
            {
                e.Handled = true;
                await SendAsync();
            }
        }

        private async void messageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await ViewModel.TriggerTypingAsync(messageTextBox.Text).ConfigureAwait(false);
        }

        private async void messageTextBox_Paste(object sender, TextControlPasteEventArgs e)
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    e.Handled = true;
                    var items = (await dataPackageView.GetStorageItemsAsync()).OfType<StorageFile>();
                    foreach (var item in items)
                    {
                        await uploadItems.AddStorageFileAsync(item);
                    }

                    return;
                }

                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    e.Handled = true;
                    var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Strings.RandomString(12)}.png");

                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    using (var bmp = await (await dataPackageView.GetBitmapAsync()).OpenReadAsync())
                    {
                        var decoder = await BitmapDecoder.CreateAsync(bmp);
                        using (var softwareBmp = await decoder.GetSoftwareBitmapAsync())
                        {
                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetSoftwareBitmap(softwareBmp);
                            await encoder.FlushAsync();
                        }
                    }

                    showPhotoPicker.Begin();
                    await uploadItems.AddStorageFileAsync(file, true);

                    return;
                }
            }
            catch (Exception ex)
            {
                HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "PasteFailure" });
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to upload.",
                    "Whoops, something went wrong while uploading that file, sorry!");
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            messageTextBox.Focus(FocusState.Keyboard);
            await SendAsync();
        }

        private async Task SendAsync()
        {
            if (ViewModel.FileUploads.Any())
            {
                uploadProgress.Visibility = Visibility.Visible;
                var progress = new Progress<double?>(d =>
                {
                    if (d == null && !uploadProgress.IsIndeterminate)
                    {
                        uploadProgress.IsIndeterminate = true;
                    }
                    else
                    {
                        uploadProgress.Value = d.Value;
                    }
                });

                await ViewModel.SendMessageAsync(messageTextBox, progress);
            }
            else
            {
                await ViewModel.SendMessageAsync(messageTextBox);
            }

            uploadProgress.Visibility = Visibility.Collapsed;
        }

        private void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (photoPicker.Visibility == Visibility.Visible)
            {
                hidePhotoPicker.Begin();
            }
            else
            {
                photoPicker.Visibility = Visibility.Visible;
                showPhotoPicker.Begin();

                loadingImagesRing.IsActive = true;

                try
                {
                    var queryOption = new QueryOptions(CommonFileQuery.OrderByDate, new string[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov" }) { FolderDepth = FolderDepth.Deep };
                    var photosQuery = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOption);
                    var factory = new FileInformationFactory(photosQuery, ThumbnailMode.SingleItem, 256);
                    photosList.ItemsSource = factory.GetVirtualizedFilesVector();
                }
                catch (Exception ex)
                {
                    HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "FileQueryFailure" });
                }

                loadingImagesRing.IsActive = false;
            }
        }

        private async void SelectionHandler(object o, SelectionChangedEventArgs ev)
        {
            hidePhotoPicker.Begin();

            foreach (var item in ev.AddedItems.OfType<IStorageFile>())
            {
                await uploadItems.AddStorageFileAsync(item);
            }
        }

        private async void OpenPopoutButton_Click(object sender, RoutedEventArgs e)
        {
            cameraPreview.Stop();
            cameraPreview.CameraHelper.Dispose();
            
            var capture = new CameraCaptureUI();
            var file = await capture.CaptureFileAsync(CameraCaptureUIMode.PhotoOrVideo);
            if (file != null)
            {
                var fileName = $"Unicord_{DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{Path.GetExtension(file.Path)}";
                var folder = App.RoamingSettings.Read("SavePhotos", true) ? KnownFolders.CameraRoll : ApplicationData.Current.TemporaryFolder;
                await file.MoveAsync(folder, fileName, NameCollisionOption.GenerateUniqueName);

                hidePhotoPicker.Begin();
                await uploadItems.AddStorageFileAsync(file);                
            }
            else
            {
                await cameraPreview.StartAsync();
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            ElementSoundPlayer.Play(ElementSoundKind.Invoke);

            var softwareBitmap = _videoFrame?.SoftwareBitmap;
            var fileName = $"Unicord_{DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.jpg";
            var file = await (App.RoamingSettings.Read("SavePhotos", true) ? KnownFolders.CameraRoll : ApplicationData.Current.TemporaryFolder)
                .CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            if (softwareBitmap != null)
            {
                // Why...
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }
            }

            if (file != null)
            {
                hidePhotoPicker.Begin();
                await uploadItems.AddStorageFileAsync(file);
            }
        }

        private async void openLocalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    CommitButtonText = $"Upload",
                    ViewMode = PickerViewMode.Thumbnail
                };

                picker.FileTypeFilter.Add("*");

                var files = await picker.PickMultipleFilesAsync();

                hidePhotoPicker.Begin();
                foreach (var file in files)
                {
                    await uploadItems.AddStorageFileAsync(file);
                }
            }
            catch { }
        }

        private void CameraPreview_PreviewFailed(object sender, PreviewFailedEventArgs e)
        {
            _error = e.Error;
            loadingCameraRing.IsActive = false;
            previewFailed.Visibility = Visibility.Visible;
        }

        private async void ShowPhotoPicker_Completed(object sender, object e)
        {
            previewFailed.Visibility = Visibility.Collapsed;
            loadingCameraRing.IsActive = true;

            await cameraPreview.StartAsync();

            loadingCameraRing.IsActive = false;
            cameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
        }

        private void HidePhotoPicker_Completed(object sender, object e)
        {
            cameraPreview.Stop();
            cameraPreview.CameraHelper.FrameArrived -= CameraHelper_FrameArrived;
            loadingCameraRing.IsActive = false;
            previewFailed.Visibility = Visibility.Collapsed;
        }

        private void CameraHelper_FrameArrived(object sender, FrameEventArgs e)
        {
            _videoFrame = e.VideoFrame;
        }

        private async void PreviewFailed_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_error != null)
            {
                await UIUtilities.ShowErrorDialogAsync("Unable to load camera", _error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            hidePhotoPicker.Begin();
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FileUploads.Remove((sender as FrameworkElement).DataContext as FileUploadModel);
        }

        private void DoubleAnimation_Completed(object sender, object e)
        {
            photosList.ItemsSource = null;
            photoPicker.Visibility = Visibility.Collapsed;
            uploadsTransform.Y = 0;
            loadingImagesRing.IsActive = false;
        }

        // TODO: Refactor and componentize
        private async Task AddToJumpListAsync()
        {
            try
            {
                if (JumpList.IsSupported())
                {
                    var list = await JumpList.LoadCurrentAsync();
                    var guild = _viewModel.Channel.Guild;

                    var data = ApplicationData.Current.LocalFolder;
                    var folder = await data.CreateFolderAsync("recents", CreationCollisionOption.OpenIfExists);

                    string group = null;
                    var arguments = $"-channelId={_viewModel.Channel.Id}"; ;
                    StorageFile file = null;

                    if (ViewModel.Channel.Guild != null)
                    {
                        group = "Recent Channels";

                        file = await folder.CreateFileAsync($"server-{guild.IconHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(guild.IconUrl + "?size=32"), file);
                    }
                    else if (_viewModel.Channel is DiscordDmChannel dm && dm.Type == ChannelType.Private)
                    {
                        group = "Recent People";

                        file = await folder.CreateFileAsync($"user-{dm.Recipient.AvatarHash}.png", CreationCollisionOption.ReplaceExisting);
                        await Tools.DownloadToFileAsync(new Uri(dm.Recipient.GetAvatarUrl(ImageFormat.Png, 32)), file);
                    }

                    if (group != null && arguments != null && file != null)
                    {
                        var item = list.Items.FirstOrDefault(i => i.Arguments == arguments);
                        if (item == null)
                        {
                            item = JumpListItem.CreateWithArguments(arguments, _viewModel.TitleText);
                            item.GroupName = group;
                            item.Logo = new Uri($"ms-appdata:///local/recents/{file.Name}");
                            list.Items.Add(item);
                        }

                        if (list.Items.Count > 10)
                        {
                            list.Items.Remove(list.Items.First());
                        }
                    }

                    await list.SaveAsync();
                }
            }
            catch { }
        }

        private async void emoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                EnsureEmotePicker();

                var pane = InputPane.GetForCurrentView();
                var flyout = FlyoutBase.GetAttachedFlyout(emoteButton);

                if (button.IsChecked == true)
                {
                    flyout?.ShowAt(button);
                    pane.TryHide();

                    _emotePicker.Visibility = Visibility.Visible;
                    await _emotePicker.Load();
                }
                else
                {
                    flyout?.Hide();
                    _emotePicker.Unload();
                    _emotePicker.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Lazily initializes the emote picker
        /// </summary>
        private void EnsureEmotePicker()
        {
            if (_emotePicker == null)
            {
                _emotePicker = new EmotePicker
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                _emotePicker.EmojiPicked += EmotePicker_EmojiPicked;
            }

            var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(emoteButton);

            if (Window.Current.CoreWindow.Bounds.Width > 400)
            {
                if (footerGrid.Children.Contains(_emotePicker))
                {
                    footerGrid.Children.Remove(_emotePicker);
                }

                _emotePicker.Width = 300;
                _emotePicker.Height = 300;
                _emotePicker.Padding = new Thickness(0);
                _emotePicker.Visibility = Visibility.Visible;

                if (flyout == null)
                {
                    flyout = new Flyout { Content = _emotePicker };
                    flyout.Closed += (o, ev) =>
                    {
                        emoteButton.IsChecked = false;
                        _emotePicker.Unload();
                    };

                    FlyoutBase.SetAttachedFlyout(emoteButton, flyout);
                }
                else if (flyout.Content != _emotePicker)
                {
                    flyout.Content = _emotePicker;
                }

            }
            else
            {
                if (flyout != null)
                {
                    (flyout as Flyout).Content = null;
                }

                _emotePicker.Width = double.NaN;
                _emotePicker.Height = 275;
                _emotePicker.Visibility = Visibility.Collapsed;
                _emotePicker.Padding = new Thickness(10, 0, 10, 0);

                if (!footerGrid.Children.Contains(_emotePicker))
                {
                    Grid.SetRow(_emotePicker, 4);
                    footerGrid.Children.Add(_emotePicker);
                }
            }

            _emotePicker.Channel = ViewModel.Channel;
        }

        private void EmotePicker_EmojiPicked(object sender, DiscordEmoji e)
        {
            if (e != null)
            {
                if (messageTextBox.Text.Length > 0 && !char.IsWhiteSpace(messageTextBox.Text[messageTextBox.Text.Length - 1]))
                    messageTextBox.Text += " ";

                messageTextBox.Text += $"{e} ";
                messageTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void messageTextBox_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            if (sender.FocusState != FocusState.Programmatic)
            {
                emoteButton.IsChecked = false;
                if (_emotePicker != null)
                {
                    _emotePicker.Unload();
                    _emotePicker.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void pinsButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePane(typeof(PinsPage), ViewModel.Channel);
        }

        private void UserListButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsPaneOpen)
            {
                OpenPane(typeof(UserListPage), ViewModel.Channel);
            }
            else
            {
                ClosePane();
            }
        }

        private void ShowSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            var page = this.FindParent<DiscordPage>();
            if (page != null)
            {
                page.ToggleSplitPane();
            }
        }

        private void Content_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsPaneOpen && ActualWidth <= 1024)
            {
                ClosePane();
            }

            var page = this.FindParent<DiscordPage>();
            if (page != null)
            {
                page.CloseSplitPane();
            }
        }

        private void OpenPane(Type t = null, object parameter = null)
        {
            IsPaneOpen = true;
            sidebarGrid.Visibility = Visibility.Visible;
            if (ActualWidth <= 1024)
            {
                OpenPaneStoryboard.Begin();
            }

            if (t != null && sidebarFrame.CurrentSourcePageType != t)
            {
                sidebarFrame.Navigate(t, parameter);
            }
        }

        private void ClosePane()
        {
            IsPaneOpen = false;
            if (ActualWidth > 1024)
                sidebarGrid.Visibility = Visibility.Collapsed;
            ClosePaneStoryboard.Begin();

            sidebarFrame.Navigate(typeof(Page));
        }

        public void TogglePane(Type t = null, object parameter = null)
        {
            if (IsPaneOpen && sidebarFrame.CurrentSourcePageType == t)
            {
                ClosePane();
            }
            else
            {
                OpenPane(t, parameter);
            }
        }

        private void ClosePaneStoryboard_Completed(object sender, object e)
        {
            sidebarGrid.Visibility = Visibility.Collapsed;
        }
    }
}
