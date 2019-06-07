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
using Windows.Foundation.Metadata;
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

        private void ThemesSettingsPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ThemesSettingsModel model)
            {
                if (e.PropertyName == nameof(model.SelectedTheme))
                {
                    var read = App.LocalSettings.Read<Theme>("SelectedTheme", null);
                    var selectedTheme = model.SelectedTheme as Theme;
                    if (read?.Name != selectedTheme?.Name)
                    {
                        _changedTheme = true;
                        relaunchRequired.Visibility = Visibility.Visible;

                        App.LocalSettings.Save("SelectedTheme", selectedTheme);

                        //preview.Resources = new ResourceDictionary();
                        //await Themes.LoadAsync(selectedTheme, preview.Resources);
                    }
                }

                if (e.PropertyName == nameof(model.ColourScheme))
                {
                    _changedTheme = true;
                    relaunchRequired.Visibility = Visibility.Visible;
                }
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            var autoRestart = ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync");
            if (_changedTheme && autoRestart)
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
                    await ThemeManager.InstallFromFileAsync(file);
                }
                catch (Exception ex)
                {
                    await UIUtilities.ShowErrorDialogAsync("Failed to install theme!", ex.Message);
                }
            }

            (DataContext as ThemesSettingsModel).ReloadThemes();
        }

        private async void RemoveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as ThemesSettingsModel;
            if (model.SelectedTheme is Theme theme)
            {
                await ThemeManager.RemoveThemeAsync(theme.Name);
                model.ReloadThemes();
            }
        }
    }
}
