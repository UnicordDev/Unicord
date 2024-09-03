using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Controls
{
    public sealed class UsernameControl : Control
    {
        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(UserViewModel), typeof(UsernameControl), new PropertyMetadata(null));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(UsernameControl), new PropertyMetadata(16));

        public UsernameControl()
        {
            this.DefaultStyleKey = typeof(UsernameControl);
        }
    }
}
