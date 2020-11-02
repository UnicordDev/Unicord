using System;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Controls;
using WamWooWam.Core;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Models
{
    public class FileUploadModel : IDisposable
    {
        public IStorageFile StorageFile { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FileName { get; set; }
        public ulong Length { get; set; }

        public bool IsTemporary { get; set; }
        public bool CanEdit { get; set; }
        public bool TranscodeFailed { get; set; }

        public bool Spoiler { get; set; } = false;
        public string DisplayLength => Files.SizeSuffix((long)Length);

        public static async Task<FileUploadModel> FromStorageFileAsync(IStorageFile file, BasicProperties prop = null, bool isTemporary = false, bool transcodeFailed = false)
        {
            Analytics.TrackEvent("FileUploadModel_CreateFromStorageFile");

            var model = new FileUploadModel();
            await model.UpdateFromStorageFileAsync(file, prop, isTemporary, transcodeFailed);
            return model;
        }

        public async Task UpdateFromStorageFileAsync(IStorageFile file, BasicProperties prop = null, bool isTemporary = false, bool transcodeFailed = false)
        {
            if (IsTemporary && StorageFile != null)
            {
                await StorageFile.DeleteAsync();
            }

            FileName = file.Name;
            IsTemporary = isTemporary;
            TranscodeFailed = transcodeFailed;

            prop = prop ?? await file.GetBasicPropertiesAsync();
            Length = prop.Size;

            if (file.ContentType.StartsWith("video"))
            {
                CanEdit = true;
            }

            if (file is IStorageItemProperties props)
            {
                using (var thumbStream = await props.GetThumbnailAsync(ThumbnailMode.SingleItem, 256))
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(thumbStream);

                    Thumbnail = image;
                }
            }

            StorageFile = file;
        }

        internal async Task<IInputStream> GetStreamAsync()
        {
            if (StorageFile != null)
                return await StorageFile.OpenReadAsync();

            return null;
        }

        public virtual void Dispose()
        {

        }
    }

    public class EditedFileUploadModel : FileUploadModel, IDisposable
    {
        public EditedFileUploadModel(FileUploadModel original)
        {
            StorageFile = original.StorageFile;
            Thumbnail = original.Thumbnail;
            FileName = original.FileName;
            Length = original.Length;
            IsTemporary = original.IsTemporary;
            Spoiler = original.Spoiler;
            CanEdit = original.CanEdit;
        }

        public UploadItemsControl Parent { get; set; }
        public MediaComposition Composition { get; set; }
        public MediaClip Clip { get; set; }
        public StorageFile CompositionFile { get; internal set; }

        public override void Dispose()
        {
            Composition = null;
            Clip = null;
        }
    }
}
