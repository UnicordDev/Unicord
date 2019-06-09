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
        private string _initialTheme;
        private int _initialColour;

        public ThemesSettingsPage()
        {
            InitializeComponent();

            _initialTheme = App.LocalSettings.Read("SelectedThemeName", string.Empty);
            if (string.IsNullOrWhiteSpace(_initialTheme))
                _initialTheme = "Default";

            _initialColour = (int)App.LocalSettings.Read("RequestedTheme", ElementTheme.Default);

            (DataContext as ThemesSettingsModel).PropertyChanged += ThemesSettingsPage_PropertyChanged;
        }

        private async void ThemesSettingsPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ThemesSettingsModel model)
            {
                var theme = model.SelectedTheme as Theme;
                if (model.ColourScheme != _initialColour || theme?.Name != _initialTheme)
                {
                    _changedTheme = true;
                    relaunchRequired.Visibility = Visibility.Visible;
                }
                else
                {
                    _changedTheme = false;
                    relaunchRequired.Visibility = Visibility.Collapsed;
                }


                var dictionary = new ResourceDictionary();
                if (!string.IsNullOrWhiteSpace(theme?.Name))
                {
                    try { await ThemeManager.LoadAsync(theme.Name, dictionary); } catch { }
                }

                // if we invert the theme then set it properly, the element will redraw and reload
                // it's resources. as far as i know there's no better way to do this.

                switch ((ElementTheme)model.ColourScheme)
                {
                    case ElementTheme.Light:
                        preview.RequestedTheme = ElementTheme.Dark;
                        break;
                    case ElementTheme.Dark:
                        preview.RequestedTheme = ElementTheme.Light;
                        break;
                    default:
                        preview.RequestedTheme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                        break;
                }

                preview.Resources = dictionary;
                preview.RequestedTheme = (ElementTheme)model.ColourScheme;
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (DataContext is ThemesSettingsModel model && model.SelectedTheme is Theme theme)
            {
                App.LocalSettings.Save("SelectedThemeName", theme.IsDefault ? string.Empty : theme.Name);
            }

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

            await (DataContext as ThemesSettingsModel).ReloadThemes();
        }

        private async void RemoveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as ThemesSettingsModel;
            if (model.SelectedTheme is Theme theme)
            {
                await ThemeManager.RemoveThemeAsync(theme.Name);
                await model.ReloadThemes();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ThemesSettingsModel model)
            {
                await model.ReloadThemes();
            }
        }
    }
}
