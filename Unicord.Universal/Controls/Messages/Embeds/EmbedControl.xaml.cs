using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls.Messages
{

    public sealed partial class EmbedControl : UserControl
    {
        public EmbedViewModel ViewModel
        {
            get { return (EmbedViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(EmbedViewModel), typeof(EmbedControl), new PropertyMetadata(null));

        public EmbedControl()
        {
            this.InitializeComponent();
        }
    }
}
