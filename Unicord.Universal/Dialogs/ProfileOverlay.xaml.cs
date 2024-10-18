using Unicord.Universal.Models.User;
using Unicord.Universal.Services;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ProfileOverlay : UserControl
    {
        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(UserViewModel), typeof(ProfileOverlay), new PropertyMetadata(null, OnUserChanged));

        private static void OnUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (ProfileOverlay)d;
            overlay.Bindings.Update();
        }

        public ProfileOverlay()
        {
            InitializeComponent();
        }

        private void DropShadowPanel_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                OverlayService.GetForCurrentView()
                    .CloseOverlay();
            }
        }
    }
}
