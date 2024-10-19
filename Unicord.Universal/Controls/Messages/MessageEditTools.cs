using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    public sealed class MessageEditTools : Control
    {
        public MessageEditViewModel ViewModel
        {
            get { return (MessageEditViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MessageEditViewModel), typeof(MessageEditTools), new PropertyMetadata(null));

        public MessageEditTools()
        {
            this.DefaultStyleKey = typeof(MessageEditTools);
        }
    }
}
