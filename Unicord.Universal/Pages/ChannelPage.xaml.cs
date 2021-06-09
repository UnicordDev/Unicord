using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Commands;
using Unicord.Universal.Controls;
using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Integration;
using Unicord.Universal.Interop;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Services;
using Unicord.Universal.Shared;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class ChannelPage : Page, INotifyPropertyChanged
    {
        private readonly List<ChannelViewModel> _channelHistory
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
            }

            UploadItems.IsEnabledChanged += UploadItems_IsEnabledChanged;
            MessageList.AddHandler(TappedEvent, new TappedEventHandler(MessageList_Tapped), true);

            VisualStateManager.GoToState(this, "NormalMode", false);
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

                var windowHandle = WindowingService.Current.GetHandle(this);
                if (model != null)
                {
                    _channelHistory.Remove(model);
                }
                else
                {
                    model = new ChannelViewModel(chan, windowHandle);
                }

                var args = this.FindParent<MainPage>()?.Arguments;
                WindowingService.Current.HandleTitleBarForControl(TopGrid);
                WindowingService.Current.SetWindowChannel(windowHandle, chan.Id);
                model.TruncateMessages();

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

        internal void FocusTextBox()
        {
            MessageTextBox.Focus();
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
                scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollViewer.ManipulationMode = ManipulationModes.All;

                var swipeService = SwipeOpenService.GetForCurrentView();
                swipeService.AddAdditionalElement(MessageList);
                swipeService.AddAdditionalElement(scrollViewer);

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

            //  await BackgroundNotificationService.GetForCurrentView().SetActiveChannelAsync(ViewModel.Channel.Id);

            try
            {
                await Dispatcher.AwaitableRunAsync(() =>
                {
                    LoadingProgress.Visibility = Visibility.Visible;
                    LoadingProgress.IsIndeterminate = true;

                    NoMessages.Visibility = Visibility.Collapsed;
                });

                if (ViewModel.Channel.Guild?.IsSynced == false)
                {
                    await ViewModel.Channel.Guild.SyncAsync().ConfigureAwait(false);
                }

                await ViewModel.LoadMessagesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: port
                Logger.LogError(ex);
            }

            await Dispatcher.AwaitableRunAsync(() =>
            {
                LoadingProgress.Visibility = Visibility.Collapsed;
                LoadingProgress.IsIndeterminate = false;
            }).ConfigureAwait(false);

            if (ViewModel.Channel.ReadState?.Unread == true)
            {
                var id = ViewModel.Channel.ReadState.LastMessageId;
                var message = ViewModel.Messages.FirstOrDefault(m => m.Id == id) ?? ViewModel.Messages.FirstOrDefault();
                if (message != null)
                {
                    await Dispatcher.AwaitableRunAsync(() =>
                        MessageList.ScrollIntoView(message, ScrollIntoViewAlignment.Leading)).ConfigureAwait(false);
                }
            }

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
            if (!(this.FindParent<DiscordPage>()?.IsWindowVisible ?? false))
                return;

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
                    await ViewModel.LoadMessagesBeforeAsync().ConfigureAwait(false);
                }
            }
        }

        private async void OnMessageTextBoxPaste(object sender, TextControlPasteEventArgs e)
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    Analytics.TrackEvent("ChannelPage_StorageItemsFromPaste");

                    e.Handled = true;
                    var items = (await dataPackageView.GetStorageItemsAsync()).OfType<StorageFile>();
                    foreach (var item in items)
                    {
                        await UploadItems.AddStorageFileAsync(item);
                    }

                    return;
                }

                if (dataPackageView.Contains("DeviceIndependentBitmapV5"))
                {
                    try
                    {
                        var data = (IRandomAccessStream)(await dataPackageView.GetDataAsync("DeviceIndependentBitmapV5"));
                        var file = await BitmapInterop.GetFromRandomAccessStreamAsync(data);
                        await UploadItems.AddStorageFileAsync(file, true);
                        return;
                    }
                    catch
                    {

                    }
                }

                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    Analytics.TrackEvent("ChannelPage_ImageFromPaste");

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
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to upload.",
                    "Whoops, something went wrong while uploading that file, sorry!");
            }
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

                    await ViewModel.SendMessageAsync(progress).ConfigureAwait(false);
                }
                else
                {
                    await ViewModel.SendMessageAsync().ConfigureAwait(false);
                }

                await Dispatcher.AwaitableRunAsync(() => UploadProgress.Visibility = Visibility.Collapsed);
            }
            catch (Exception ex)
            {
                // just in case, should realistically never happen
                Logger.LogError(ex);
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
            if ((bool)e.NewValue)
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
                    var queryOption = new QueryOptions(CommonFileQuery.OrderByDate, new string[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".gif" });
                    queryOption.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 256, ThumbnailOptions.UseCurrentScale);
                    queryOption.FolderDepth = FolderDepth.Deep;
                    queryOption.IndexerOption = IndexerOption.UseIndexerWhenAvailable;

                    var photosQuery = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOption);
                    var factory = new FileInformationFactory(photosQuery, ThumbnailMode.PicturesView, 256, ThumbnailOptions.UseCurrentScale, true);
                    PhotosList.ItemsSource = factory.GetVirtualizedFilesVector();
                }
                catch (Exception ex)
                {
                    // TODO: Port
                    Logger.LogError(ex);
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
                var fileName = $"Unicord_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(file.Path)}";
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
                Analytics.TrackEvent("ChannelPage_ImageFromPhotosList");
                await UploadItems.AddStorageFileAsync(item);
            }
        }

        private void ChannelPage_OnDragEnter(object sender, DragEventArgs e)
        {

        }

        private void ChannelPage_OnDragOver(object sender, DragEventArgs e)
        {
            if (ViewModel.CanUpload)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Send to {ViewModel.FullChannelName}";
                e.DragUIOverride.IsCaptionVisible = true;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void ChannelPage_OnDragLeave(object sender, DragEventArgs e)
        {

        }

        private async void ChannelPage_OnDrop(object sender, DragEventArgs e)
        {
            if (!ViewModel.CanUpload)
                return;

            if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                Analytics.TrackEvent("ChannelPage_ImageFromDrop");

                var file = await Tools.GetImageFileFromDataPackage(e.DataView);
                await UploadItems.AddStorageFileAsync(file, true);

                return;
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                Analytics.TrackEvent("ChannelPage_LinkFromDrop");

                var link = await e.DataView.GetWebLinkAsync();
                MessageTextBox.AppendText(link.ToString());

                return;
            }

            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                Analytics.TrackEvent("ChannelPage_TextFromDrop");

                var text = await e.DataView.GetTextAsync();
                MessageTextBox.AppendText(text);

                return;
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                Analytics.TrackEvent("ChannelPage_FilesFromDrop");

                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items.OfType<IStorageFile>())
                {
                    await UploadItems.AddStorageFileAsync(item, false);
                }
            }
        }

        private async void OnOpenLocalButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    CommitButtonText = $"Upload to {_viewModel.FullChannelName}",
                    ViewMode = PickerViewMode.Thumbnail
                };

                picker.FileTypeFilter.Add("*");

                var files = await picker.PickMultipleFilesAsync();

                HidePhotoPicker.Begin();
                foreach (var file in files)
                {
                    Analytics.TrackEvent("ChannelPage_FilesFromPicker");
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

        private void pinsButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePane(typeof(PinsPage), ViewModel.Channel);
        }

        private void UserListButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePane(typeof(UserListPage), ViewModel.Channel);
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
            Analytics.TrackEvent("ChannelPage_EnterEditMode");

            _viewModel.IsEditMode = true;
            VisualStateManager.GoToState(this, "EditMode", true);

            //if (message != null)
            //{
            //    MessageList.SelectedItems.Add(message);
            //}
        }

        private void LeaveEditMode()
        {
            Analytics.TrackEvent("ChannelPage_LeaveEditMode");

            _viewModel.IsEditMode = false;
            VisualStateManager.GoToState(this, "NormalMode", true);
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
                Analytics.TrackEvent("ChannelPage_MassDeleteMessage");
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
            Analytics.TrackEvent("ChannelPage_OpenPane" + t.Name);

            var helper = SwipeOpenService.GetForCurrentView().Helper;
            if (helper != null)
                helper.IsEnabled = false;

            IsPaneOpen = true;
            SidebarGrid.Visibility = Visibility.Visible;
            if (ActualWidth <= (1024 - 276))
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
            Analytics.TrackEvent("ChannelPage_ClosePane");

            //var helper = SwipeOpenService.GetForCurrentView().Helper;
            //if (helper != null)
            //    helper.IsEnabled = true;

            IsPaneOpen = false;
            if (ActualWidth > (1024 - 276))
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

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePane(typeof(SearchPage), ViewModel.Channel);
        }

        private void MessageList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsPaneOpen && ActualWidth <= (1024 - 276))
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
            if (WindowingService.Current.Supported)
            {
                var handle = WindowingService.Current.GetHandle(this);
                var newHandle = await WindowingService.Current.OpenChannelWindowAsync(_viewModel.Channel, handle);
                if (newHandle != null)
                {
                    WindowingService.Current.SetWindowChannel(handle, 0);

                    var service = DiscordNavigationService.GetForCurrentView();
                    await service.NavigateAsync(null, true);

                    _viewModel.Dispose();
                    _channelHistory.Remove(_viewModel);
                }
            }
        }

        private async void NewCompactWindowButton_Click(object sender, RoutedEventArgs e)
        {
            //if (WindowManager.MultipleWindowsSupported)
            //{
            //    WindowManager.SetChannelForCurrentWindow(0);
            //    await WindowManager.OpenChannelWindowAsync(_viewModel.Channel, ApplicationViewMode.CompactOverlay);

            //    var service = DiscordNavigationService.GetForCurrentView();
            //    await service.NavigateAsync(null, true);

            //    _viewModel.Dispose();
            //    _channelHistory.Remove(_viewModel);
            //}
        }

        private void HideTitleBar_Completed(object sender, object e)
        {
            TopGrid.Visibility = Visibility.Collapsed;
            FooterGrid.Visibility = Visibility.Collapsed;
        }

        private async void MessageTextBox_ShouldSendTyping(object sender, EventArgs e)
        {
            await ViewModel.TriggerTypingAsync().ConfigureAwait(false);
        }

        private void MessageTextBox_EditInvoked(object sender, EventArgs e)
        {
            var lastMessage = _viewModel.Messages.LastOrDefault(m => m.Author.IsCurrent);
            if (lastMessage != null)
            {
                var container = MessageList.ContainerFromItem(lastMessage);
                if (container != null)
                {
                    MessageList.ScrollIntoView(lastMessage, ScrollIntoViewAlignment.Leading);
                    var viewer = container.FindChild<MessageControl>();
                    viewer?.BeginEdit();
                }
            }
        }

        private async void MessageTextBox_SendInvoked(object sender, string e)
        {
            await SendAsync();
        }

        private async void PinToStartItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
