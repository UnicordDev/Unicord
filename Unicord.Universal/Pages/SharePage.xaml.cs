using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Integration;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class SharePage : Page
    {
        private ShareOperation _shareOperation;
        private StorageFile _file;

        public SharePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ShareOperation op)
            {
                _shareOperation = op;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // BUGBUG: this is messy
                var items = new List<object> { new { Name = "Direct Messages" } };
                items.AddRange(App.Discord.Guilds.Values.OrderBy(g => g.Name));
                guildBox.ItemsSource = items;

                channelsBox.ItemTemplateSelector = new ChannelTemplateSelector()
                {
                    ServerChannelTemplate = (DataTemplate)App.Current.Resources["NoIndicatorChannelListTemplate"],
                    DirectMessageTemplate = (DataTemplate)App.Current.Resources["NoIndicatorDMChannelTemplate"]
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    var contact = _shareOperation.Contacts.FirstOrDefault();
                    if (contact != null)
                    {
                        var id = await ContactListManager.TryGetChannelIdAsync(contact);
                        if (id != 0)
                        {
                            guildBox.SelectedIndex = 0;
                            channelsBox.SelectedItem = await App.Discord.CreateDmChannelAsync(id);

                            target.Text = contact.DisplayName;
                            destinationGrid.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                _shareOperation.ReportStarted();

                var data = _shareOperation.Data;
                if (data.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                {
                    _file = (await data.GetStorageItemsAsync()).FirstOrDefault() as StorageFile;

                    var img = new BitmapImage();
                    thumbnailImage.Source = img;
                    await img.SetSourceAsync(await _file.GetThumbnailAsync(ThumbnailMode.SingleItem));

                    return;
                }
                else if (data.AvailableFormats.Contains(StandardDataFormats.Bitmap))
                {
                    // do shit

                    _file = await Tools.GetImageFileFromDataPackage(data);

                    var img = new BitmapImage();
                    thumbnailImage.Source = img;
                    await img.SetSourceAsync(await _file.GetThumbnailAsync(ThumbnailMode.PicturesView));

                    return;
                }
                else
                {
                    thumbnailImage.Visibility = Visibility.Collapsed;
                    captionText.PlaceholderText = "Type a message!";
                }

                if (data.AvailableFormats.Contains(StandardDataFormats.Text))
                {
                    var text = await data.GetTextAsync();
                    captionText.Text += text;
                }

                if (data.AvailableFormats.Contains(StandardDataFormats.WebLink))
                {
                    var text = await data.GetWebLinkAsync();
                    captionText.Text += text;
                }
            }
            finally
            {
                _shareOperation.ReportDataRetrieved();
                this.FindParent<MainPage>().HideConnectingOverlay();

                if (_file != null)
                {
                    var maxSize = (ulong)(App.Discord.CurrentUser.UploadLimit());
                    var props = await _file.GetBasicPropertiesAsync();
                    if (props.Size >= maxSize)
                    {
                        await UIUtilities.ShowErrorDialogAsync("This file is too big!", $"We're gonna need something under {(Files.SizeSuffix((long)maxSize, 0))} please!");
                        Window.Current.Close();
                    }
                }
            }
        }

        private void guildBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (guildBox.SelectedIndex == 0)
            {
                channelsListSource.IsSourceGrouped = false;
                channelsListSource.Source = App.Discord.PrivateChannels.Values
                    .OrderBy(c => c.Name ?? c.Recipients[0]?.Username)
                    .OrderByDescending(m => m.ReadState?.LastMessageId ?? 0);
            }
            else
            {
                var guild = guildBox.SelectedItem as DiscordGuild;
                var user = guild.CurrentMember;
                channelsListSource.Source = guild.Channels.Values
                    .Where(c => c.PermissionsFor(user).HasFlag(Permissions.SendMessages))
                    .OrderBy(c => c.Position);
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (channelsBox.SelectedItem is DiscordChannel channel)
                {
                    if (_file != null)
                    {
                        overlay.Visibility = Visibility.Visible;
                        ring.Value = 0;
                        subtext.Opacity = 1;

                        var progress = new Progress<double?>(p => ring.Value = p ?? 0d);
                        var stream = await _file.OpenReadAsync();
                        var dictionary = new Dictionary<string, IInputStream>() { [_file.Name] = stream };
                        await Tools.SendFilesWithProgressAsync(channel, captionText.Text, dictionary, progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(captionText.Text))
                    {
                        await channel.SendMessageAsync(captionText.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync("Sending failed!", ex.Message);
                _shareOperation.ReportError(ex.Message);
                return;
            }

            _shareOperation.ReportCompleted();
            //_shareOperation.DismissUI();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Window.Current.Close();
        }
    }

    public class ChannelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DirectMessageTemplate { get; set; }
        public DataTemplate ServerChannelTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is DiscordDmChannel)
            {
                return DirectMessageTemplate;
            }

            return ServerChannelTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
