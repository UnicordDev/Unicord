using Microsoft.AppCenter.Crashes;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls
{
    public sealed class FileViewer : Control
    {
        private SemaphoreSlim _semaphore;
        private bool _appliedTemplate;

        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register("File", typeof(StorageFile), typeof(FileViewer), new PropertyMetadata(null, PropertyChanged));

        public StorageFile File { get => (StorageFile)GetValue(FileProperty); set => SetValue(FileProperty, value); }

        public FileViewer()
        {
            DefaultStyleKey = typeof(FileViewer);
            _semaphore = new SemaphoreSlim(1, 1);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _appliedTemplate = true;
        }

        private static async void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = d as FileViewer;

            if (!v._appliedTemplate)
            {
                v._appliedTemplate = v.ApplyTemplate();
            }

            await v.OnFilePropertyChanged(e);
        }

        public async Task OnFilePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (e.NewValue is StorageFile f && GetTemplateChild("image") is Image image)
                {
                    if (!(image.Source is BitmapImage source))
                    {
                        source = new BitmapImage();
                        image.Source = source;
                    }

                    ToolTipService.SetToolTip(this, f.Name);

                    using (var thumb = await f.GetThumbnailAsync(ThumbnailMode.PicturesView, 256, ThumbnailOptions.UseCurrentScale))
                    {
                        if (thumb != null)
                        {
                            await source.SetSourceAsync(thumb);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Debug.WriteLine(ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}