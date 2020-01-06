using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ManageThemeDialog : ContentDialog
    {
        public ManageThemeDialog()
        {
            this.InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveEnter.Begin();
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void YesRemoveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            HideDeleteButton.Begin();
            ProgressRing.IsActive = true;

            await ThemeManager.RemoveThemeAsync((DataContext as Theme).Name);
            this.Hide();
        }

        private void NoRemoveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ShareButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Visible;
            RemoveExit.Begin();
        }

        private void RemoveEnter_Completed(object sender, object e)
        {
            ShareButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void RemoveExit_Completed(object sender, object e)
        {

        }
    }
}
