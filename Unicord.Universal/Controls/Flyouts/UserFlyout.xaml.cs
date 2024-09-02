using DSharpPlus.Entities;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserFlyout : AdaptiveFlyout
    {
        public UserFlyout(object param) : base(param)
        {
            InitializeComponent();
        }

        // i dislike this
        private void IconLabelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
