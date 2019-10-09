using DSharpPlus.Entities;
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
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
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
        private List<ChannelViewModel> _channelHistory
            = new List<ChannelViewModel>();

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
        private DispatcherTimer _titleBarTimer;

        public event PropertyChangedEventHandler PropertyChanged;

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
                EmoteButton.AddAccelerator(VirtualKey.E, VirtualKeyModifiers.Control);
                PinsButton.AddAccelerator(VirtualKey.P, VirtualKeyModifiers.Control);
                UserListButton.AddAccelerator(VirtualKey.U, VirtualKeyModifiers.Control);
                UploadButton.AddAccelerator(VirtualKey.U, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
                SearchButton.AddAccelerator(VirtualKey.F, VirtualKeyModifiers.Control);
                NewWindowButton.AddAccelerator(VirtualKey.N, VirtualKeyModifiers.Control);
            }

            UploadItems.IsEnabledChanged += UploadItems_IsEnabledChanged;
            MessageTextBox.KeyDown += messageTextBox_KeyDown;
            MessageList.AddHandler(TappedEvent, new TappedEventHandler(MessageList_Tapped), true);
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

                var args = this.FindParent<MainPage>()?.Arguments;
                WindowManager.HandleTitleBarForControl(TopGrid);
                WindowManager.SetChannelForCurrentWindow(chan.Id);

                ViewModel = model;
                DataContext = ViewModel;

                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
                    MessageTextBox.Focus(FocusState.Keyboard);

                while (_channelHistory.Count > 10)
                {
                    var oldModel = _channelHistory.ElementAt(0);
                    oldModel.Dispose();
                    _channelHistory.RemoveAt(0);
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
                var scrollViewer = MessageList.FindChild<ScrollViewer>("ScrollViewer");
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

                ShowSidebarButtonContainer.Visibility = this.FindParent<DiscordPage>() == null ? Visibility.Collapsed : Visibility.Visible;

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

            FullscreenService.GetForCurrentView()?.LeaveFullscreen();
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
                    LoadingProgress.Visibility = Visibility.Visible;
                    LoadingProgress.IsIndeterminate = true;

                    NoMessages.Visibility = Visibility.Collapsed;
                });
                
                if (ViewModel.Channel.Guild?.IsSynced == false && ViewModel.Channel.Guild.IsLarge)
                {
                    await ViewModel.Channel.Guild.SyncAsync().ConfigureAwait(false);
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
                LoadingProgress.Visibility = Visibility.Collapsed;
                LoadingProgress.IsIndeterminate = false;
            }).ConfigureAwait(false);

            if (IsPaneOpen)
            {
                await Dispatcher.AwaitableRunAsync(() =>
                    SidebarFrame.Navigate(SidebarFrame.CurrentSourcePageType, ViewModel.Channel)).ConfigureAwait(false);
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
                    var message = MessageList.Items.LastOrDefault() as DiscordMessage;
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
                    var container = MessageList.ContainerFromItem(lastMessage);
                    if (container != null)
                    {
                        MessageList.ScrollIntoView(lastMessage, ScrollIntoViewAlignment.Leading);
                        var viewer = container.FindChild<MessageViewer>();
                        viewer.BeginEditing();
                    }
                }
            }
        }

        private async void messageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await ViewModel.TriggerTypingAsync(MessageTextBox.Text).ConfigureAwait(false);
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
                        await UploadItems.AddStorageFileAsync(item);
                    }

                    return;
                }

                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    e.Handled = true;
                    var file = await Tools.GetImageFileFromDataPackage(dataPackageView);
                    await UploadItems.AddStorageFileAsync(file, true);

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
            MessageTextBox.Focus(FocusState.Keyboard);
            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Focus(FocusState.Keyboard);
            await SendAsync();
        }

        private async Task SendAsync()
        {
            try
            {
                if (ViewModel.FileUploads.Any())
                {
                    UploadProgress.Visibility = Visibility.Visible;
                    var progress = new Progress<double?>(d =>
                    {
                        if (d == null && !UploadProgress.IsIndeterminate)
                        {
                            UploadProgress.IsIndeterminate = true;
                        }
                        else
                        {
                            UploadProgress.Value = d.Value;
                        }
                    });

                    await ViewModel.SendMessageAsync(MessageTextBox, progress).ConfigureAwait(false);
                }
                else
                {
                    await ViewModel.SendMessageAsync(MessageTextBox).ConfigureAwait(false);
                }

                await Dispatcher.AwaitableRunAsync(() => UploadProgress.Visibility = Visibility.Collapsed);
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
            TopGrid.Visibility = Visibility.Visible;
            FooterGrid.Visibility = Visibility.Visible;
            _titleBarTimer?.Stop();

            HideTitleBar.Stop();
            ShowTitleBar.Begin();
        }

        private void BeginHideTitleBar()
        {
            ShowTitleBar.Stop();
            HideTitleBarAnimation.To = -TopGrid.RenderSize.Height;
            HideTitleBar.Begin();
        }

        private void UploadItems_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                UploadItems.Visibility = Visibility.Visible;
                HideUploadPanel.Stop();
                ShowUploadPanel.Begin();
            }
            else
            {
                ShowUploadPanel.Stop();
                HideUploadPanel.Begin();
            }
        }

        private void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (PhotoPicker.Visibility == Visibility.Visible)
            {
                HidePhotoPicker.Begin();
            }
            else
            {
                PhotoPicker.Visibility = Visibility.Visible;
                ShowPhotoPicker.Begin();

                LoadingImagesRing.IsActive = true;

                try
                {
                    var queryOption = new QueryOptions(CommonFileQuery.OrderByDate, new string[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".gif" }) { FolderDepth = FolderDepth.Deep };
                    var photosQuery = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOption);
                    var factory = new FileInformationFactory(photosQuery, ThumbnailMode.SingleItem, 256);
                    PhotosList.ItemsSource = factory.GetVirtualizedFilesVector();
                }
                catch (Exception ex)
                {
                    // TODO: Port
                    // HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "FileQueryFailure" });
                }

                LoadingImagesRing.IsActive = false;
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

                HidePhotoPicker.Begin();
                await UploadItems.AddStorageFileAsync(file);
            }
        }

        private async void PhotosList_ItemClick(object sender, ItemClickEventArgs e)
        {
            HidePhotoPicker.Begin();

            if (e.ClickedItem is IStorageFile item)
            {
                await UploadItems.AddStorageFileAsync(item);
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

                HidePhotoPicker.Begin();
                foreach (var file in files)
                {
                    await UploadItems.AddStorageFileAsync(file);
                }
            }
            catch { }
        }

        private void ShowPhotoPicker_Completed(object sender, object e)
        {

        }

        private void HidePhotoPicker_Completed(object sender, object e)
        {
            PhotosList.ItemsSource = null;
            LoadingImagesRing.IsActive = false;
            PhotoPicker.Visibility = Visibility.Collapsed;
        }

        private void ShowUploadPanel_Completed(object sender, object e)
        {
            // just to make sure
            UploadItems.Visibility = Visibility.Visible;
        }

        private void HideUploadPanel_Completed(object sender, object e)
        {
            PhotoPicker.Visibility = Visibility.Collapsed;
            UploadItems.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HidePhotoPicker.Begin();
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
            var flyout = FlyoutBase.GetAttachedFlyout(EmoteButton);

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

            var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(EmoteButton);

            if (Window.Current.CoreWindow.Bounds.Width > 400)
            {
                if (FooterGrid.Children.Contains(_emotePicker))
                {
                    FooterGrid.Children.Remove(_emotePicker);
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
                        EmoteButton.IsChecked = false;
                        _emotePicker.Unload();
                    };

                    FlyoutBase.SetAttachedFlyout(EmoteButton, flyout);
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

                if (!FooterGrid.Children.Contains(_emotePicker))
                {
                    Grid.SetRow(_emotePicker, 4);
                    FooterGrid.Children.Add(_emotePicker);
                }
            }

            _emotePicker.Channel = ViewModel.Channel;
        }

        private void EmotePicker_EmojiPicked(object sender, DiscordEmoji e)
        {
            if (e != null)
            {
                if (MessageTextBox.Text.Length > 0 && !char.IsWhiteSpace(MessageTextBox.Text[MessageTextBox.Text.Length - 1]))
                {
                    MessageTextBox.Text += " ";
                }

                MessageTextBox.Text += $"{e} ";
                MessageTextBox.Select(MessageTextBox.Text.Length, 0);
                MessageTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void messageTextBox_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            if (sender.FocusState != FocusState.Programmatic)
            {
                EmoteButton.IsChecked = false;
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

            MessageList.SelectionMode = ListViewSelectionMode.Multiple;

            if (message != null)
            {
                MessageList.SelectedItems.Add(message);
            }

            MessageList.ItemTemplate = (DataTemplate)Resources["EditingMessageTemplate"];
            ShowEditControls.Begin();
        }

        private void LeaveEditMode()
        {
            _viewModel.IsEditMode = false;

            MessageList.SelectionMode = ListViewSelectionMode.None;
            MessageList.ItemTemplate = (DataTemplate)Resources["DefaultMessageTemplate"];
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
                var items = MessageList.SelectedItems.OfType<DiscordMessage>().ToArray();

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
                    MessageList.SelectedItems.Remove(item);
                }
            }
        }

        private void OpenPane(Type t = null, object parameter = null)
        {
            IsPaneOpen = true;
            SidebarGrid.Visibility = Visibility.Visible;
            if (Window.Current.Bounds.Width <= 1024)
            {
                OpenPaneStoryboard.Begin();
            }

            if (t != null && SidebarFrame.CurrentSourcePageType != t)
            {
                SidebarFrame.Navigate(t, parameter);
            }
        }

        private void ClosePane()
        {
            IsPaneOpen = false;
            if (Window.Current.Bounds.Width > 1024)
            {
                SidebarGrid.Visibility = Visibility.Collapsed;
            }

            ClosePaneStoryboard.Begin();

            SidebarFrame.Navigate(typeof(Page));
        }

        public void TogglePane(Type t = null, object parameter = null)
        {
            if (IsPaneOpen && SidebarFrame.CurrentSourcePageType == t)
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
            SidebarGrid.Visibility = Visibility.Collapsed;
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
            //this.FindParent<DiscordPage>().OpenCustomPane(typeof(ChannelEditPage), _viewModel.Channel);
        }

        private async void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowManager.MultipleWindowsSupported)
            {
                WindowManager.SetChannelForCurrentWindow(0);
                await WindowManager.OpenChannelWindowAsync(_viewModel.Channel);

                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(null);

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

                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(null);

                _viewModel.Dispose();
                _channelHistory.Remove(_viewModel);
            }
        }

        private void HideTitleBar_Completed(object sender, object e)
        {
            TopGrid.Visibility = Visibility.Collapsed;
            FooterGrid.Visibility = Visibility.Collapsed;
        }
    }
}
