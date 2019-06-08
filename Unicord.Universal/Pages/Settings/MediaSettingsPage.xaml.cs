using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class MediaSettingsPage : Page
    {
        public MediaSettingsPage()
        {
            InitializeComponent();
            DataContext = new MediaSettingsModel();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MediaSettingsModel model && e.AddedItems.FirstOrDefault() is string str)
            {
                model.Resolution = str;
            }
        }
    }
}
