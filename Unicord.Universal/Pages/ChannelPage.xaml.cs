﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using CommunityToolkit.Mvvm.Input;
using Unicord.Universal.Controls;
using Unicord.Universal.Integration;
using Unicord.Universal.Interop;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Emoji;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation;
using Unicord.Universal.Extensions;

namespace Unicord.Universal.Pages
{
    public sealed partial class ChannelPage : Page, INotifyPropertyChanged
    {
        private readonly List<ChannelPageViewModel> _channelHistory
            = new List<ChannelPageViewModel>();

        public ChannelPageViewModel ViewModel
        {
            get => _viewModel;
            private set
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }

        public bool IsPaneOpen { get; private set; }

        private ChannelPageViewModel _viewModel;
        private bool _scrollHandlerAdded;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChannelPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 5))
            {
                if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 6))
                    KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

                this.AddAccelerator(VirtualKey.D, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, EditMode_Invoked);
            }

            UploadItems.IsEnabledChanged += UploadItems_IsEnabledChanged;
            MessageList.AddHandler(TappedEvent, new TappedEventHandler(MessageList_Tapped), true);

            VisualStateManager.GoToState(this, "NormalMode", false);
            VisualStateManager.GoToState(Header, "NormalMode", true);
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
            if (e.Parameter is not DiscordChannel chan)
                return;

            Application.Current.Suspending += OnSuspending;

            var navigation = SystemNavigationManager.GetForCurrentView();
            navigation.BackRequested += Navigation_BackRequested;

            var enterEditMode = new RelayCommand<MessageViewModel>(EnterEditMode);
            var exitEditMode = new RelayCommand(LeaveEditMode);

            if (_viewModel?.IsEditMode == true)
            {
                LeaveEditMode();
            }

            var model = _channelHistory.FirstOrDefault(c => c.Channel.Id == chan.Id && !c.IsDisposed);
            if (ViewModel != null)
            {
                await ViewModel.TruncateMessagesAsync();
                _channelHistory.Add(ViewModel);
            }

            var windowHandle = WindowingService.Current.GetHandle(this);
            if (model != null)
            {
                _channelHistory.Remove(model);
            }
            else
            {
                model = new ChannelPageViewModel(chan, windowHandle, enterEditMode, exitEditMode);
            }

            WindowingService.Current.SetWindowChannel(windowHandle, chan.Id);
            await model.TruncateMessagesAsync();

            ViewModel = model;
            DataContext = ViewModel;

            // TODO: this check should be for the input method, not platform
            if (SystemPlatform.Desktop)
                MessageTextBox.Focus(FocusState.Keyboard);

            while (_channelHistory.Count > 10)
            {
                var oldModel = _channelHistory.ElementAt(0);
                oldModel.Dispose();
                _channelHistory.RemoveAt(0);
            }

            await Load().ConfigureAwait(false);
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

                _scrollHandlerAdded = true;
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

                var old = ViewModel;
                ViewModel = last;
                DataContext = ViewModel;

                old.Dispose();

                await Load().ConfigureAwait(false);
            }
        }

        private async Task Load()
        {
            ViewModel.LastAccessed = DateTimeOffset.Now;
            try
            {
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

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (ViewModel.ReadState.Unread == true)
                {
                    var id = ViewModel.ReadState.LastMessageId;
                    var message = ViewModel.Messages.FirstOrDefault(m => m.Id == id) ?? ViewModel.Messages.FirstOrDefault();
                    if (message != null)
                    {
                        MessageList.ScrollIntoView(message, ScrollIntoViewAlignment.Leading);
                    }
                }
            });

            await JumpListManager.AddToListAsync(_viewModel);
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scroll = sender as ScrollViewer;

            var window = WindowingService.Current.GetHandle(this);
            if (!WindowingService.Current.IsActive(window))
                return;

            if (!e.IsIntermediate)
            {
                if (scroll.VerticalOffset >= (scroll.ScrollableHeight - scroll.ViewportHeight) && ViewModel.ReadState.Unread != false)
                {
                    var message = ViewModel.Messages.LastOrDefault();
                    if (message != null)
                    {
                        await message.Message.AcknowledgeAsync().ConfigureAwait(false);
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

                    if (PhotoPicker.Visibility == Visibility.Visible)
                        HidePhotoPicker.Begin();

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
                        var data = (IRandomAccessStream)await dataPackageView.GetDataAsync("DeviceIndependentBitmapV5");
                        var file = await BitmapInterop.GetFromRandomAccessStreamAsync(data);

                        if (PhotoPicker.Visibility == Visibility.Visible)
                            HidePhotoPicker.Begin();
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

                    if (PhotoPicker.Visibility == Visibility.Visible)
                        HidePhotoPicker.Begin();

                    var file = await Tools.GetImageFileFromDataPackage(dataPackageView);
                    await UploadItems.AddStorageFileAsync(file, true);

                    return;
                }
            }
            catch (Exception ex)
            {
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
                await ViewModel.SendMessageAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // just in case, should realistically never happen
                Logger.LogError(ex);
            }
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
                ShowPhotoPicker.Begin();

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

        private void EnterEditMode(MessageViewModel message)
        {
            Analytics.TrackEvent("ChannelPage_EnterEditMode");

            _viewModel.IsEditMode = true;
            VisualStateManager.GoToState(this, "EditMode", true);
            VisualStateManager.GoToState(Header, "EditMode", true);

            if (message != null)
            {
                MessageList.SelectedItems.Add(message);
            }
        }

        private void LeaveEditMode()
        {
            Analytics.TrackEvent("ChannelPage_LeaveEditMode");

            _viewModel.IsEditMode = false;
            VisualStateManager.GoToState(this, "NormalMode", true);
            VisualStateManager.GoToState(Header, "NormalMode", true);
        }

        private void MessageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems.OfType<MessageViewModel>())
            {
                if (!item.DeleteCommand.CanExecute(null))
                {
                    MessageList.SelectedItems.Remove(item);
                }
            }
        }

        private void MessageList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //var page = this.FindParent<DiscordPage>();
            //if (page != null)
            //{
            //    page.CloseSplitPane();
            //}

            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down)
            {
                _viewModel.EnterEditModeCommand?.Execute(null);
            }
        }

        private void EditMode_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!_viewModel.IsEditMode)
            {
                _viewModel.EnterEditModeCommand?.Execute(null);
            }
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
                MessageList.ScrollIntoView(lastMessage, ScrollIntoViewAlignment.Leading);
                lastMessage.IsEditing = true;
            }
        }

        private async void MessageTextBox_SendInvoked(object sender, string e)
        {
            await SendAsync();
        }

        private MessageViewModel _reactionModel;
        internal void ShowReactionPicker(MessageViewModel model)
        {
            var control = MessageList.ContainerFromItem(model);
            if (control is not FrameworkElement element) return;

            _reactionModel = model;
            EmoteFlyout.ShowAt(element);
        }

        private void EmotePicker_EmojiPicked(object sender, EmojiViewModel e)
        {
            if (_reactionModel != null)
            {
                _reactionModel.ReactCommand.Execute(e);
            }

            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) != CoreVirtualKeyStates.Down)
            {
                _reactionModel = null;
                EmoteFlyout.Hide();
            }
        }
    }
}
