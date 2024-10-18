using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Extensions
{
    public static class ShiftEnterExtensions
    {
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ShiftEnterExtensions), new PropertyMetadata(false, OnIsEnabledPropertyChanged));

        public static ICommand GetShiftEnterCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ShiftEnterCommandProperty);
        }

        public static void SetShiftEnterCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ShiftEnterCommandProperty, value);
        }

        public static readonly DependencyProperty ShiftEnterCommandProperty =
            DependencyProperty.RegisterAttached("ShiftEnterCommand", typeof(ICommand), typeof(ShiftEnterExtensions), new PropertyMetadata(null));

        public static ICommand GetEnterCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(EnterCommandProperty);
        }

        public static void SetEnterCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(EnterCommandProperty, value);
        }

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.RegisterAttached("EnterCommand", typeof(ICommand), typeof(ShiftEnterExtensions), new PropertyMetadata(null));

        public static ICommand GetEscapeCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(EscapeCommandProperty);
        }

        public static void SetEscapeCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(EscapeCommandProperty, value);
        }

        public static readonly DependencyProperty EscapeCommandProperty =
            DependencyProperty.RegisterAttached("EscapeCommand", typeof(ICommand), typeof(ShiftEnterExtensions), new PropertyMetadata(null));

        private static void OnIsEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)d;
            if ((bool)e.NewValue)
            {
                textBox.KeyDown += OnPreviewKeyDown;
            }
            else
            {
                textBox.KeyDown -= OnPreviewKeyDown;
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var textBox = (TextBox)sender;

            var shiftEnter = GetShiftEnterCommand(textBox);
            var enter = GetEnterCommand(textBox);
            var escape = GetEscapeCommand(textBox);

            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;

                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (shiftEnter.CanExecute(textBox))
                        shiftEnter.Execute(textBox);
                }
                else
                {
                    if (enter.CanExecute(textBox))
                        enter.Execute(textBox);
                }
            }

            if (e.Key == VirtualKey.Escape)
            {
                if (escape.CanExecute(textBox))
                    escape.Execute(textBox);
            }
        }
    }
}
