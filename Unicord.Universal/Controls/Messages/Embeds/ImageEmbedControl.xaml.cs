using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls.Messages
{
    public sealed partial class ImageEmbedControl : UserControl
    {
        public EmbedImageViewModel ViewModel
        {
            get { return (EmbedImageViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(EmbedImageViewModel), typeof(ImageEmbedControl), new PropertyMetadata(null));

        public ImageEmbedControl()
        {
            this.InitializeComponent();
        }

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await OverlayService.GetForCurrentView()
                                    .ShowOverlayAsync<AttachmentOverlayPage>(ViewModel);
            }
        }
    }
}
