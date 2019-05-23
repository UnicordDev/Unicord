using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

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
                    var maxSize = (ulong)(App.Discord.CurrentUser.HasNitro ? 50 * 1024 * 1024 : 8 * 1024 * 1024);
                    var props = await _file.GetBasicPropertiesAsync();
                    if (props.Size >= maxSize)
                    {
                        await UIUtilities.ShowErrorDialogAsync("This file is too big!", $"We're gonna need something under {(App.Discord.CurrentUser.HasNitro ? "50MB" : "8MB")} please!");
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
                    .OrderBy(c => c.Name ?? c.Recipient?.Username)
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
