using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Models
{
    public class FileUploadModel : IDisposable
    {
        public IStorageFile StorageFile { get; private set; }
        public ImageSource Thumbnail { get; private set; }
        public IInputStream File { get; private set; }
        public string FileName { get; private set; }
        public ulong Length { get; private set; }
        public bool IsTemporary { get; private set; }
        public bool TranscodeFailed { get; private set; }
        public string DisplayLength => Files.SizeSuffix((long)Length);

        public static async Task<FileUploadModel> FromStorageFileAsync(IStorageFile file, BasicProperties prop = null, bool isTemporary = false, bool transcodeFailed = false)
        {
            var model = new FileUploadModel
            {
                File = await file.OpenReadAsync(),
                FileName = file.Name,
                IsTemporary = isTemporary,
                TranscodeFailed = transcodeFailed
            };

            prop = prop ?? await file.GetBasicPropertiesAsync();
            model.Length = prop.Size;

            if (file is IStorageItemProperties props)
            {
                var thumbStream = await props.GetThumbnailAsync(ThumbnailMode.SingleItem);

                var image = new BitmapImage();
                await image.SetSourceAsync(thumbStream);

                model.Thumbnail = image;
            }

            model.StorageFile = file;

            return model;
        }

        public void Dispose()
        {
            File?.Dispose();
        }
    }
}
