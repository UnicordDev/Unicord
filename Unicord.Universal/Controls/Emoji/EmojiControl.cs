using Unicord.Universal.Models.Emoji;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Emoji
{
    public sealed class EmojiControl : Control
    {
        public EmojiViewModel Emoji
        {
            get => (EmojiViewModel)GetValue(EmojiProperty);
            set => SetValue(EmojiProperty, value);
        }

        public static readonly DependencyProperty EmojiProperty =
            DependencyProperty.Register("Emoji", typeof(EmojiViewModel), typeof(EmojiControl), new PropertyMetadata(default(EmojiViewModel)));

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(EmojiControl), new PropertyMetadata(32));

        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        public static readonly DependencyProperty AnimateProperty =
            DependencyProperty.Register("Animate", typeof(bool), typeof(EmojiControl), new PropertyMetadata(false));

        public EmojiControl()
        {
            this.DefaultStyleKey = typeof(EmojiControl);
        }
    }
}
