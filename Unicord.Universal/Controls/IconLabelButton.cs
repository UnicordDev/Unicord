using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls
{
    public sealed class IconLabelButton : Button
    {
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(IconLabelButton), new PropertyMetadata(null));

        public IconLabelButton()
        {
            DefaultStyleKey = typeof(IconLabelButton);
        }
    }
}
