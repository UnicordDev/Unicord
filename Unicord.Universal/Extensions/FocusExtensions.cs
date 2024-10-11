using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Extensions
{
    public static class FocusExtensions
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusExtensions), new PropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            void OnLoaded(object sender, RoutedEventArgs e)
            {
                var control = (Control)d;
                control.Focus(FocusState.Programmatic);
                control.Loaded -= OnLoaded;
            }

            var control = (Control)d;
            if ((bool)e.NewValue)
            {
                if (!control.IsLoaded)
                {
                    control.Loaded += OnLoaded;
                }
                else
                {
                    control.Focus(FocusState.Programmatic);
                }
            }
        }
    }
}
