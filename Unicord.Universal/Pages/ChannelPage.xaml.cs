using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Commands;
using Unicord.Universal.Controls;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Management;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class ChannelPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<ChannelViewModel> _channelHistory
            = new List<ChannelViewModel>();

        private EmotePicker _emotePicker;
        private ChannelViewModel _viewModel;
        private DispatcherTimer _titleBarTimer;
        private bool _scrollHandlerAdded;

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

        public ChannelPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

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
                newWindowButton.AddAccelerator(VirtualKey.N, VirtualKeyModifiers.Control);
            }

            uploadItems.IsEnabledChanged += UploadItems_IsEnabledChanged;
            messageTextBox.KeyDown += messageTextBox_KeyDown;
            messageList.AddHandler(TappedEvent, new TappedEventHandler(MessageList_Tapped), true);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            foreach (var item in _channelHistory)
            {
                if (_viewModel != item)
                    item.Dispose();
            }

            _channelHistory.RemoveAll(m => m != _viewModel);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordChannel chan)
            {
                Application.Current.Suspending += OnSuspending;

                var navigation = SystemNavigationManager.GetForCurrentView();
                navigation.BackRequested += Navigation_BackRequested;

                if (_viewModel?.IsEditMode == true)
                {
                    LeaveEditMode();
                }

                if (IsPaneOpen)
                {
                    ClosePane();
                }

                var model = _channelHistory.FirstOrDefault(c => c.Channel.Id == chan.Id && !c.IsDisposed);
                if (ViewModel != null)
                {
                    _channelHistory.Add(ViewModel);
                }

                if (model != null)
                {
                    _channelHistory.Remove(model);
                }
                else
                {
                    model = new ChannelViewModel(chan);
                } 

                WindowManager.HandleTitleBarForControl(topGrid);
                WindowManager.SetChannelForCurrentWindow(chan.Id);

                ViewModel = model;
                DataContext = ViewModel;

                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
                {
                    messageTextBox.Focus(FocusState.Keyboard);
                }

                while (_channelHistory.Count > 10)
                {
                    var oldModel = _channelHistory.ElementAt(0);
                    _channelHistory.RemoveAt(0);

                    oldModel.Dispose();
                }

                await Load();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Application.Current.Suspending -= OnSuspending;
            var navigation = SystemNavigationManager.GetForCurrentView();
            navigation.BackRequested -= Navigation_BackRequested;
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

            var args = this.FindParent<MainPage>()?.Arguments;
            if (args?.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _titleBarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _titleBarTimer.Tick += _titleBarTimer_Tick;
                DefaultControls.Visibility = Visibility.Collapsed;
                PointerEntered += ChannelPage_PointerEntered;
                PointerExited += ChannelPage_PointerExited;
            }
            else
            {
                _titleBarTimer = null;
                DefaultControls.Visibility = Visibility.Visible;
                BeginShowTitleBar();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_titleBarTimer != null)
            {
                _titleBarTimer.Stop();
                PointerEntered -= ChannelPage_PointerEntered;
                PointerExited -= ChannelPage_PointerExited;
            }
        }

        private async void Navigation_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled)
                return;

            this.FindParent<MainPage>()?.LeaveFullscreen();
            var last = _channelHistory.ElementAtOrDefault(_channelHistory.Count - 1);
            if (last != null)
            {
                e.Handled = true;

                _channelHistory.Remove(last);

                ViewModel = last;
                DataContext = ViewModel;

                await Load().ConfigureAwait(false);
            }
        }

        private async Task Load()
        {
            ViewModel.LastAccessed = DateTimeOffset.Now;

            try
            {
                await Dispatcher.AwaitableRunAsync(() =>
                {
                    loadingProgress.Visibility = Visibility.Visible;
                    loadingProgress.IsIndeterminate = true;

                    noMessages.Visibility = Visibility.Collapsed;
                }).ConfigureAwait(false);

                if (ViewModel.Channel.Guild?.IsSynced == false && ViewModel.Channel.Guild.IsLarge)
                {
                    await ViewModel.Channel.Guild.SyncAsync().ConfigureAwait(false);
                }

                if (ViewModel.Channel is DiscordDmChannel dm)
                {
                    if (dm.OngoingCall == null)
                    {
                        dm.RequestCallInfo();
                    }
                }

                await ViewModel.LoadMessagesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: port
                // HockeyClient.Current.TrackException(ex);
            }

            await Dispatcher.AwaitableRunAsync(() =>
            {
                loadingProgress.Visibility = Visibility.Collapsed;
                loadingProgress.IsIndeterminate = false;
            }).ConfigureAwait(false);

            if (IsPaneOpen)
            {
                await Dispatcher.AwaitableRunAsync(() =>
                    sidebarFrame.Navigate(sidebarFrame.CurrentSourcePageType, ViewModel.Channel)).ConfigureAwait(false);
            }

            await JumpListManager.AddToListAsync(_viewModel.Channel).ConfigureAwait(false);
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

                    await ViewModel.LoadMessagesBeforeAsync().ConfigureAwait(false);

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
                // TODO: Port
                // HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "PasteFailure" });
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
            try
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

                    await ViewModel.SendMessageAsync(messageTextBox, progress).ConfigureAwait(false);
                }
                else
                {
                    await ViewModel.SendMessageAsync(messageTextBox).ConfigureAwait(false);
                }

                await Dispatcher.AwaitableRunAsync(() => uploadProgress.Visibility = Visibility.Collapsed);
            }
            catch (Exception ex)
            {
                // just in case, should realistically never happen
                Logger.Log(ex);
            }
        }

        private void ChannelPage_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            BeginShowTitleBar();
        }

        private void ChannelPage_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _titleBarTimer?.Start();
        }

        private void _titleBarTimer_Tick(object sender, object e)
        {
            BeginHideTitleBar();
        }

        private void BeginShowTitleBar()
        {
            topGrid.Visibility = Visibility.Visible;
            footerGrid.Visibility = Visibility.Visible;
            _titleBarTimer?.Stop();

            HideTitleBar.Stop();
            ShowTitleBar.Begin();
        }

        private void BeginHideTitleBar()
        {
            ShowTitleBar.Stop();
            HideTitleBarAnimation.To = -topGrid.RenderSize.Height;
            HideTitleBar.Begin();
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
                    // TODO: Port
                    // HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "FileQueryFailure" });
                }

                loadingImagesRing.IsActive = false;
            }
        }

        private async void OpenPopoutButton_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private async void PhotosList_ItemClick(object sender, ItemClickEventArgs e)
        {
            hidePhotoPicker.Begin();

            if (e.ClickedItem is IStorageFile item)
            {
                await uploadItems.AddStorageFileAsync(item);
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

        private void ShowPhotoPicker_Completed(object sender, object e)
        {

        }

        private void HidePhotoPicker_Completed(object sender, object e)
        {
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
                await _emotePicker.Load().ConfigureAwait(false);
            }
            else
            {
                flyout?.Hide();
                _emotePicker.Unload();
                _emotePicker.Visibility = Visibility.Collapsed;
            }
        }

        // BUGBUG: this is a mess
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
                messageTextBox.Select(messageTextBox.Text.Length, 0);
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
            var loader = ResourceLoader.GetForCurrentView("ChannelPage");
            if (await UIUtilities.ShowYesNoDialogAsync(loader.GetString("MassDeleteTitle"), loader.GetString("MassDeleteMessage"), "\xE74D"))
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

        private async void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowManager.MultipleWindowsSupported)
            {
                WindowManager.SetChannelForCurrentWindow(0);
                await WindowManager.OpenChannelWindowAsync(_viewModel.Channel);

                this.FindParent<DiscordPage>().Navigate(null, new DrillInNavigationTransitionInfo());
                _viewModel.Dispose();
                _channelHistory.Remove(_viewModel);
            }
        }

        private async void NewCompactWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowManager.MultipleWindowsSupported)
            {
                WindowManager.SetChannelForCurrentWindow(0);
                await WindowManager.OpenChannelWindowAsync(_viewModel.Channel, ApplicationViewMode.CompactOverlay);

                this.FindParent<DiscordPage>().Navigate(null, new DrillInNavigationTransitionInfo());
                _viewModel.Dispose();
                _channelHistory.Remove(_viewModel);
            }
        }

        private void HideTitleBar_Completed(object sender, object e)
        {
            topGrid.Visibility = Visibility.Collapsed;
            footerGrid.Visibility = Visibility.Collapsed;
        }

        private void CallButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
