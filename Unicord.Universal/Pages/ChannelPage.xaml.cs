using DSharpPlus.Entities;
using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unicord.Universal.Commands;
using Unicord.Universal.Controls;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Management;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Core;
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
        private Dictionary<ulong, ChannelViewModel> _channelHistory
            = new Dictionary<ulong, ChannelViewModel>();

        public ChannelViewModel ViewModel
        {
            get => _viewModel;
            private set
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

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
                {
                    KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
                }

                this.AddAccelerator(VirtualKey.D, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, EditMode_Invoked);
                emoteButton.AddAccelerator(VirtualKey.E, VirtualKeyModifiers.Control);
                pinsButton.AddAccelerator(VirtualKey.P, VirtualKeyModifiers.Control);
                userListButton.AddAccelerator(VirtualKey.U, VirtualKeyModifiers.Control);
                uploadButton.AddAccelerator(VirtualKey.U, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
                searchButton.AddAccelerator(VirtualKey.F, VirtualKeyModifiers.Control);
            }

            uploadItems.IsEnabledChanged += UploadItems_IsEnabledChanged;
            messageTextBox.KeyDown += messageTextBox_KeyDown;
            messageList.AddHandler(TappedEvent, new TappedEventHandler(MessageList_Tapped), true);

            Application.Current.Suspending += OnSuspending;

            if (ApiInformation.IsTypePresent("Windows.UI.Core.SystemNavigationManager"))
            {
                var navigation = SystemNavigationManager.GetForCurrentView();
                navigation.BackRequested += Navigation_BackRequested;
            }
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            foreach (var item in _channelHistory)
            {
                if (item.Value != _viewModel)
                {
                    item.Value.Dispose();
                }
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordChannel chan)
            {
                WindowManager.HandleTitleBarForGrid(topGrid);

                if (_viewModel?.IsEditMode == true)
                {
                    LeaveEditMode();
                }

                if (IsPaneOpen)
                {
                    ClosePane();
                }

                ChannelViewModel model = null;
                WindowManager.SetChannelForCurrentWindow(chan.Id);

                if (_channelHistory.TryGetValue(chan.Id, out var result))
                {
                    model = result;
                }

                if (ViewModel != null)
                {
                    _channelHistory[ViewModel.Channel.Id] = ViewModel;
                }

                if (model == null)
                {
                    model = new ChannelViewModel(chan);
                }

                ViewModel = model;
                DataContext = ViewModel;

                while (_channelHistory.Count > 10)
                {
                    var oldest = _channelHistory.OrderBy(m => m.Value.LastAccessed.ToUnixTimeMilliseconds()).FirstOrDefault();
                    var value = _channelHistory[oldest.Key];
                    _channelHistory.Remove(oldest.Key);

                    Logger.Log($"Removing ChannelViewModel for {oldest.Value.Channel}");

                    value.Dispose();
                }

                await Load();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Application.Current.Suspending -= OnSuspending;

            if (ApiInformation.IsTypePresent("Windows.UI.Core.SystemNavigationManager"))
            {
                var navigation = SystemNavigationManager.GetForCurrentView();
                navigation.BackRequested -= Navigation_BackRequested;
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

            var lastChannel = _channelHistory.OrderBy(m => m.Value.LastAccessed).FirstOrDefault();
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

                // if (ViewModel.Channel.Guild?.IsSynced == false)
                // {
                //     await ViewModel.Channel.Guild.SyncAsync().ConfigureAwait(false);
                // }
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

            await JumpListManager.AddToListAsync(_viewModel.Channel);
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
            var textBox = (sender as TextBox);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    var start = textBox.SelectionStart;
                    textBox.Text = textBox.Text.Insert(start, "\r\n");
                    textBox.SelectionStart = start + 1;
                }
                else
                {
                    await SendAsync();
                }
            }
            else if (e.Key == VirtualKey.Up && string.IsNullOrWhiteSpace(textBox.Text))
            {
                var lastMessage = _viewModel.Messages.LastOrDefault(m => m.Author.IsCurrent);
                if (lastMessage != null)
                {
                    var container = messageList.ContainerFromItem(lastMessage);
                    if (container != null)
                    {
                        messageList.ScrollIntoView(lastMessage, ScrollIntoViewAlignment.Leading);
                        var viewer = container.FindChild<MessageViewer>();
                        viewer.BeginEditing();
                    }
                }
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
                    var file = await Tools.GetImageFileFromDataPackage(dataPackageView);
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

        internal void FocusTextBox()
        {
            messageTextBox.Focus(FocusState.Keyboard);
            messageTextBox.SelectionStart = messageTextBox.Text.Length;
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

        private void UploadItems_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                uploadItems.Visibility = Visibility.Visible;
                hideUploadPanel.Stop();
                showUploadPanel.Begin();
            }
            else
            {
                showUploadPanel.Stop();
                hideUploadPanel.Begin();
            }
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
                    var queryOption = new QueryOptions(CommonFileQuery.OrderByDate, new string[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".gif" }) { FolderDepth = FolderDepth.Deep };
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
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 32))
            {
                previewFailed.Visibility = Visibility.Collapsed;
                loadingCameraRing.IsActive = true;
                await cameraPreview.StartAsync();
                loadingCameraRing.IsActive = false;
                cameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
            }
        }

        private void HidePhotoPicker_Completed(object sender, object e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 32))
            {
                cameraPreview.Stop();
                cameraPreview.CameraHelper.FrameArrived -= CameraHelper_FrameArrived;
                loadingCameraRing.IsActive = false;
                previewFailed.Visibility = Visibility.Collapsed;
            }

            photosList.ItemsSource = null;
            loadingImagesRing.IsActive = false;
            photoPicker.Visibility = Visibility.Collapsed;
        }

        private void ShowUploadPanel_Completed(object sender, object e)
        {
            // just to make sure
            uploadItems.Visibility = Visibility.Visible;
        }

        private void HideUploadPanel_Completed(object sender, object e)
        {
            photoPicker.Visibility = Visibility.Collapsed;
            uploadItems.Visibility = Visibility.Collapsed;
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

        private async void emoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                await ShowEmojiPicker(button);
            }
        }

        private async Task ShowEmojiPicker(ToggleButton button)
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
                {
                    messageTextBox.Text += " ";
                }

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

        public void EnterEditMode(DiscordMessage message = null)
        {
            _viewModel.IsEditMode = true;

            messageList.SelectionMode = ListViewSelectionMode.Multiple;

            if (message != null)
            {
                messageList.SelectedItems.Add(message);
            }

            messageList.ItemTemplate = (DataTemplate)Resources["EditingMessageTemplate"];
            ShowEditControls.Begin();
        }

        private void LeaveEditMode()
        {
            _viewModel.IsEditMode = false;

            messageList.SelectionMode = ListViewSelectionMode.None;
            messageList.ItemTemplate = (DataTemplate)Resources["DefaultMessageTemplate"];
            HideEditControls.Begin();
        }

        private void CloseEditButton_Click(object sender, RoutedEventArgs e)
        {
            LeaveEditMode();
        }

        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (await UIUtilities.ShowYesNoDialogAsync("Delete these messages?", "Are you sure you wanna delete all these messages?", "\xE74D"))
            {
                var items = messageList.SelectedItems.OfType<DiscordMessage>().ToArray();

                LeaveEditMode();

                foreach (var item in items)
                {
                    await item.DeleteAsync();
                    await Task.Delay(500);
                }
            }
        }

        private void MessageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var command = new DeleteMessageCommand();

            foreach (var item in e.AddedItems.OfType<DiscordMessage>())
            {
                if (!command.CanExecute(item))
                {
                    messageList.SelectedItems.Remove(item);
                }
            }
        }

        private void OpenPane(Type t = null, object parameter = null)
        {
            IsPaneOpen = true;
            sidebarGrid.Visibility = Visibility.Visible;
            if (Window.Current.Bounds.Width <= 1024)
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
            if (Window.Current.Bounds.Width > 1024)
            {
                sidebarGrid.Visibility = Visibility.Collapsed;
            }

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

        private void MessageList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsPaneOpen && Window.Current.Bounds.Width <= 1024)
            {
                ClosePane();
            }

            var page = this.FindParent<DiscordPage>();
            if (page != null)
            {
                page.CloseSplitPane();
            }

            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down)
            {
                EnterEditMode();
            }
        }

        private void EditMode_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!_viewModel.IsEditMode)
            {
                EnterEditMode();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<DiscordPage>().OpenCustomPane(typeof(ChannelEditPage), _viewModel.Channel);
        }
    }
}
