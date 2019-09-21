using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
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

        public ThemesSettingsModel Model { get; }

        public ThemesSettingsPage()
        {
            Model = new ThemesSettingsModel();
            Model.PropertyChanged += ThemesSettingsPage_PropertyChanged;

            InitializeComponent();
            DataContext = Model;

            _initialTheme = App.LocalSettings.Read("SelectedThemeName", string.Empty);
            if (string.IsNullOrWhiteSpace(_initialTheme))
                _initialTheme = "Default";

            _initialColour = (int)App.LocalSettings.Read("RequestedTheme", ElementTheme.Default);
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
                if (!string.IsNullOrWhiteSpace(theme?.Name) && !theme.IsDefault)
                {
                    try { await ThemeManager.LoadAsync(theme.Name, dictionary); } catch { model.AvailableThemes.Remove(theme); }
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
            if (Model.SelectedTheme is Theme theme)
            {
                App.LocalSettings.Save("SelectedThemeName", theme.IsDefault ? string.Empty : theme.Name);
            }

            var resources = ResourceLoader.GetForCurrentView("ThemesSettingsPage");
            var autoRestart = ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync");
            if (_changedTheme && autoRestart)
            {
                if (await UIUtilities.ShowYesNoDialogAsync(resources.GetString("ThemeChangedTitle"), resources.GetString("ThemeChangedMessage")))
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

            await Model.ReloadThemes();
        }

        private async void RemoveThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedTheme is Theme theme)
            {
                await ThemeManager.RemoveThemeAsync(theme.Name);
                await Model.ReloadThemes();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var args = this.FindParent<MainPage>()?.Arguments;
            var resources = ResourceLoader.GetForCurrentView("ThemesSettingsPage");
            if (args != null && args.ThemeLoadException != null)
            {
                themeLoadError.Visibility = Visibility.Visible;
                themeLoadError.Text = string.Format(resources.GetString("ThemeLoadFailedFormat"), args.ThemeLoadException.Message);
            }

            await Model.ReloadThemes();
        }
    }
}
