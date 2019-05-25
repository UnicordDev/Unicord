using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThemesSettingsPage : Page
    {
        private bool _changedTheme;

        public ThemesSettingsPage()
        {
            InitializeComponent();
            (DataContext as ThemesSettingsModel).PropertyChanged += ThemesSettingsPage_PropertyChanged;
        }

        private async void ThemesSettingsPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ThemesSettingsModel model && e.PropertyName == nameof(model.SelectedTheme))
            {
                _changedTheme = true;

                App.LocalSettings.Save("SelectedTheme", model.SelectedTheme as Theme);

                preview.Resources = new ResourceDictionary();
                await Themes.LoadAsync(model.SelectedTheme as Theme, preview.Resources);
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_changedTheme)
            {
                if (await UIUtilities.ShowYesNoDialogAsync("Theme changed!", "In order to update your theme, Unicord must restart. Do you want to restart now?"))
                {
                    await CoreApplication.RequestRestartAsync("");
                }
            }
        }

        private async void InstallThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".uni-theme");
            picker.FileTypeFilter.Add(".zip");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    await Themes.InstallFromFileAsync(file);
                }
                catch (Exception ex)
                {
                    await UIUtilities.ShowErrorDialogAsync("Failed to install theme!", ex.Message);
                }
            }
        }
    }
}
