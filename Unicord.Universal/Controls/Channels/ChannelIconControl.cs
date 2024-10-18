using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Unicord.Universal.Models.Channels;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Channels
{
    public sealed class ChannelIconControl : Control
    {
        public ChannelViewModel Channel
        {
            get { return (ChannelViewModel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(ChannelViewModel), typeof(ChannelIconControl), new PropertyMetadata(null));

        public ChannelIconControl()
        {
            this.DefaultStyleKey = typeof(ChannelIconControl);
        }
    }
}
