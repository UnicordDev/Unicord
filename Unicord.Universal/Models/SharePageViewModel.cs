using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage.FileProperties;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Models
{
    internal class SharePageGuildViewModel : ViewModelBase
    {
        private GuildViewModel _guildViewModel;

        public SharePageGuildViewModel()
        {

        }

        public SharePageGuildViewModel(DiscordGuild guild)
        {
            _guildViewModel = new GuildViewModel(guild.Id, this);
        }

        public IEnumerable<ChannelViewModel> GetAccessibleChannels()
        {
            if (_guildViewModel != null)
            {
                return _guildViewModel.AccessibleChannels;
            }
            else
            {
                return discord.PrivateChannels
                    .OrderBy(c => c.Value.LastMessageId)
                    .Select(c => new DmChannelListViewModel(c.Value));
            }
        }
    }


    internal class SharePageViewModel : ViewModelBase
    {
        private ShareOperation _operation;
        private StorageFile _file;
        private BitmapImage _image;
        private string _text;
        public SharePageViewModel(ShareOperation operation)
        {
            this._operation = operation;
        }

        public ObservableCollection<SharePageGuildViewModel> Guilds { get; }
        public BitmapImage Thumbnail { get => _image; set => OnPropertySet(ref _image, value); }
        public string Text { get => _text; set => OnPropertySet(ref _text, value); }

        private async Task LoadAsync()
        {
            _operation.ReportStarted();
            try
            {
                var data = _operation.Data;
                if (data.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                {
                    _file = (await data.GetStorageItemsAsync()).FirstOrDefault() as StorageFile;

                    var img = new BitmapImage();
                    Thumbnail = img;
                    await img.SetSourceAsync(await _file.GetThumbnailAsync(ThumbnailMode.SingleItem));
                    return;
                }
                else if (data.AvailableFormats.Contains(StandardDataFormats.Bitmap))
                {
                    _file = await Tools.GetImageFileFromDataPackage(data);

                    var img = new BitmapImage();
                    Thumbnail = img;
                    await img.SetSourceAsync(await _file.GetThumbnailAsync(ThumbnailMode.PicturesView));

                    return;
                }

                if (data.AvailableFormats.Contains(StandardDataFormats.Text))
                {
                    var text = await data.GetTextAsync();
                    Text += text;
                }

                if (data.AvailableFormats.Contains(StandardDataFormats.WebLink))
                {
                    var text = await data.GetWebLinkAsync();
                    Text += text;
                }
            }
            finally
            {
                _operation.ReportDataRetrieved();
            }
        }
    }
}
