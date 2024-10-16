using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Models
{
    public class FileUploadModel : ViewModelBase, IDisposable
    {
        public ChannelPageViewModel Parent { get; }

        public IStorageFile StorageFile { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public string FileName { get; set; }
        public ulong Length { get; set; }

        public bool IsTemporary { get; set; }
        public bool CanCrop { get; set; }
        public bool CanEdit { get; set; }
        public bool TranscodeFailed { get; set; }

        public bool Spoiler { get; set; } = false;
        public string DisplayLength => Tools.ToFileSizeString(Length);

        internal FileUploadModel(ChannelPageViewModel viewModel)
        {
            Parent = viewModel;
            EditCommand = new AsyncRelayCommand(EditAsync);
            CropCommand = new AsyncRelayCommand(CropAsync);
        }

        public ICommand EditCommand { get; }
        public ICommand CropCommand { get; }

        public static async Task<FileUploadModel> FromStorageFileAsync(ChannelPageViewModel viewModel, IStorageFile file, BasicProperties prop = null, bool isTemporary = false, bool transcodeFailed = false)
        {
            Analytics.TrackEvent("FileUploadModel_CreateFromStorageFile");

            var model = new FileUploadModel(viewModel);
            await model.UpdateFromStorageFileAsync(file, prop, isTemporary, transcodeFailed);
            return model;
        }

        public async Task UpdateFromStorageFileAsync(IStorageFile file, BasicProperties prop = null, bool isTemporary = false, bool transcodeFailed = false)
        {
            if (file is IStorageFilePropertiesWithAvailability availability && !availability.IsAvailable)
                throw new InvalidOperationException("The selected file is unavailable.");
            
            if (IsTemporary && StorageFile != null)
            {
                await StorageFile.DeleteAsync();
            }

            StorageFile = file;
            FileName = file.Name;
            IsTemporary = isTemporary;
            TranscodeFailed = transcodeFailed;

            prop = prop ?? await file.GetBasicPropertiesAsync();
            Length = prop.Size;

            if (file.ContentType.StartsWith("video"))
            {
                CanEdit = true;
            }

            if (file.ContentType.StartsWith("image"))
            {
                CanCrop = true;
            }

            if (file is IStorageItemProperties props && (Thumbnail?.Dispatcher.HasThreadAccess ?? true))
            {
                using var thumbStream = await props.GetThumbnailAsync(ThumbnailMode.SingleItem, 256);
                await (Thumbnail ??= new BitmapImage()).SetSourceAsync(thumbStream);
            }
        }

        internal async Task<IInputStream> GetStreamAsync()
        {
            if (StorageFile != null)
                return await StorageFile.OpenReadAsync();
            return null;
        }

        private async Task EditAsync()
        {
            var newModel = new EditedFileUploadModel(this, Parent);
            Parent.FileUploads.Remove(this);
            Parent.FileUploads.Add(newModel);

            await OverlayService.GetForCurrentView()
                .ShowOverlayAsync<VideoEditor>(newModel);
        }

        private async Task CropAsync()
        {
            var newFile = await StorageFile.CopyAsync(ApplicationData.Current.LocalFolder, FileName, NameCollisionOption.GenerateUniqueName);
            var props = await newFile.Properties.GetImagePropertiesAsync();

            var sourceToken = SharedStorageAccessManager.AddFile(StorageFile);
            var destinationToken = SharedStorageAccessManager.AddFile(newFile);

            var options = new LauncherOptions { TargetApplicationPackageFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe" };

            var parameters = new ValueSet
            {
                { "EllipticalCrop", false },
                { "ShowCamera", false },
                { "InputToken", sourceToken },
                { "DestinationToken", destinationToken }
            };

            var result = await Launcher.LaunchUriForResultsAsync(new Uri("microsoft.windows.photos.crop:"), options, parameters);
            if (result.Status != LaunchUriStatus.Success)
            {
                // TODO: Dialog
                return;
            }

            await UpdateFromStorageFileAsync(newFile);
        }

        public virtual void Dispose()
        {

        }
    }

    public class EditedFileUploadModel : FileUploadModel, IDisposable
    {
        public EditedFileUploadModel(FileUploadModel original, ChannelPageViewModel viewModel)
            : base(viewModel)
        {
            StorageFile = original.StorageFile;
            Thumbnail = original.Thumbnail;
            FileName = original.FileName;
            Length = original.Length;
            IsTemporary = original.IsTemporary;
            Spoiler = original.Spoiler;
            CanEdit = original.CanEdit;
            CanCrop = original.CanCrop;
        }

        public SynchronizationContext SyncContext
            => syncContext;

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
