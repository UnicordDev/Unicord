using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.IO;
using System.Diagnostics;
using System.Threading;

#if WINDOWS_UWP

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls
{

#elif WINDOWS_WPF

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Unicord.Desktop.Controls
{

#endif
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

#if WINDOWS_UWP
        protected override void OnApplyTemplate()
#elif WINDOWS_WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            _appliedTemplate = true;
        }

        private static async void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = d as FileViewer;

            if (!v._appliedTemplate)
                v._appliedTemplate = v.ApplyTemplate();

            await v._semaphore.WaitAsync();

            if (e.NewValue is StorageFile f && v.GetTemplateChild("image") is Image image)
            {
                if (!(image.Source is BitmapImage source))
                {
                    source = new BitmapImage();
                    image.Source = source;
                }

                try
                {
                    var thumb = await f.GetThumbnailAsync(ThumbnailMode.SingleItem, 256);
                    if (thumb != null)
#if WINDOWS_UWP
                        await source.SetSourceAsync(thumb);
#elif WINDOWS_WPF
                        source.StreamSource = thumb.AsStreamForRead();
#endif
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                v._semaphore.Release();
            }
        }
    }
}
