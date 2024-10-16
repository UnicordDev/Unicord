using Unicord.Universal.Models.Messages;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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
