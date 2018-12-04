using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.HockeyApp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Controls;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
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

        private EmotePicker _emotePicker;
        private ChannelViewModel _viewModel;
        private bool _loading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChannelPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            messageTextBox.KeyDown += messageTextBox_KeyDown;

            if (App.StatusBarFill != default)
            {
                topGrid.Padding = App.StatusBarFill;
            }

            if (_emotePicker == null)
            {
                if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile" && ActualWidth > 400)
                {
                    _emotePicker = new EmotePicker() { Width = 300, Height = 300, HorizontalAlignment = HorizontalAlignment.Stretch };

                    var flyout = new Flyout { Content = _emotePicker };
                    flyout.Closed += (o, ev) =>
                    {
                        emoteButton.IsChecked = false;
                        _emotePicker.Unload();
                    };

                    FlyoutBase.SetAttachedFlyout(emoteButton, flyout);
                }
                else
                {
                    if (ApiInformation.IsTypePresent("Windows.UI.Core.SystemNavigationManager"))
                    {
                        var navigation = SystemNavigationManager.GetForCurrentView();
                        navigation.BackRequested += Navigation_BackRequested;
                    }

                    _emotePicker = new EmotePicker()
                    {
                        Height = 275,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Visibility = Visibility.Collapsed,
                        Padding = new Thickness(10, 0, 10, 0)
                    };
                    Grid.SetRow(_emotePicker, 4);
                    footerGrid.Children.Add(_emotePicker);
                }

                _emotePicker.EmojiPicked += EmotePicker_EmojiPicked;
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

                if(_channelHistory.Value.Count > 10)
                {
                    var oldest = _channelHistory.Value.OrderBy(m => m.Value.LastAccessed).FirstOrDefault();
                    _channelHistory.Value.TryRemove(oldest.Key, out var value);
                    value.Dispose();
                }

                await Load();
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = messageList.FindChild<ScrollViewer>("ScrollViewer");
            scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            await Load();
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

                    if (!ViewModel.FileUploads.Any())
                    {
                        uploadItems.Visibility = Visibility.Collapsed;
                    }

                    _emotePicker.Channel = ViewModel.Channel;
                    noMessages.Visibility = Visibility.Collapsed;
                    splitView.IsPaneOpen = false;
                });

                await ViewModel.LoadMessagesAsync();

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

            await AddToJumpListAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            try
            {
                if (e.Channel.Id == ViewModel.Channel.Id)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (noMessages.Visibility == Visibility.Visible)
                            noMessages.Visibility = Visibility.Collapsed;

                        messageList.Items.Add(e.Message);
                    });
                }
            }
            catch (Exception ex)
            {
                HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "ChannelLoadFailure" });
            }
        }

        private async Task Discord_MessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Channel.Id == ViewModel.Channel.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //var message = messagesPanel.Children
                    //    .OfType<MessageViewer>()
                    //    .ToArray()
                    //    .FirstOrDefault(m => m.Id == e.Message.Id);
                    //if (message != null)
                    //{
                    //    message.UpdateViewer(MessageViewer.MessageProperty, e.MessageBefore, e.Message);
                    //}
                });
            }
        }

        private async Task Discord_MessageDeleted(MessageDeleteEventArgs e)
        {
            if (e.Channel.Id == ViewModel.Channel.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    messageList.Items.Remove(e.Message);
                });
            }
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
                else if (scroll.VerticalOffset <= 150 && !_loading)
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
                    uploadItems.Visibility = Visibility.Visible;
                    foreach (var item in items)
                    {
                        await uploadItems.AddStorageFileAsync(item);
                    }
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

                    uploadItems.Visibility = Visibility.Visible;
                    await uploadItems.AddStorageFileAsync(file, true);
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
                uploadItems.Visibility = Visibility.Collapsed;
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
                photosList.SelectionChanged -= SelectionHandler;
                cameraPreview.FileChosen -= FileHandler;
            }
            else
            {
                photoPicker.Visibility = Visibility.Visible;
                showPhotoPicker.Begin();

                loadingImagesRing.IsActive = true;

                try
                {
                    var queryOption = new QueryOptions(CommonFileQuery.OrderByDate, new string[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov" }) { FolderDepth = FolderDepth.Deep };
                    var query = KnownFolders.PicturesLibrary
                        .CreateFileQueryWithOptions(queryOption);

                    var factory = new FileInformationFactory(query, ThumbnailMode.PicturesView, 128);
                    photosList.ItemsSource = factory.GetVirtualizedFilesVector();
                }
                catch (Exception ex)
                {
                    HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "FileQueryFailure" });
                }

                photosList.SelectionChanged += SelectionHandler;

                loadingImagesRing.IsActive = false;
            }

            async void SelectionHandler(object o, SelectionChangedEventArgs ev)
            {
                photosList.SelectionChanged -= SelectionHandler;
                cameraPreview.FileChosen -= FileHandler;
                hidePhotoPicker.Begin();

                uploadItems.Visibility = Visibility.Visible;
                await uploadItems.AddStorageFileAsync(ev.AddedItems.First() as IStorageFile);
            }

            async void FileHandler(object o, StorageFile file)
            {
                photosList.SelectionChanged -= SelectionHandler;
                cameraPreview.FileChosen -= FileHandler;
                hidePhotoPicker.Begin();

                uploadItems.Visibility = Visibility.Visible;
                await uploadItems.AddStorageFileAsync(file);
            }

            cameraPreview.FileChosen += FileHandler;
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

        private void EmotePicker_EmojiPicked(object sender, DiscordEmoji e)
        {
            if (e != null)
            {
                if (!char.IsWhiteSpace(messageTextBox.Text.LastOrDefault()))
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
                _emotePicker.Unload();
                _emotePicker.Visibility = Visibility.Collapsed;
            }
        }

        private void pinsButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
            if (splitView.IsPaneOpen)
                splitViewPane.Navigate(typeof(PinsPage), ViewModel.Channel);
        }
    }
}
