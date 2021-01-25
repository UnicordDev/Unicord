using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Unicord.Constants;

namespace Unicord.Universal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallThemePage : Page
    {
        private StorageFile _file;

        public InstallThemePage()
        {
            RequestedTheme = App.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is FileActivatedEventArgs args)
            {
                _file = args.Files.OfType<StorageFile>().ToList().FirstOrDefault();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowingService.Current.HandleTitleBarForWindow(TitleBar, this);
            WindowingService.Current.HandleTitleBarForControl(Title);

            var currentView = ApplicationView.GetForCurrentView();
            currentView.TryResizeView(new Size(480, 640));

            if (_file == null)
            {
                await UIUtilities.ShowErrorDialogAsync("InvalidThemeFileTitle", "InvalidThemeFileNotFound");
                Window.Current.Close();

                return;
            }

            try
            {
                var resources = new ResourceDictionary();
                var themes = await ThemeManager.LoadFromArchiveAsync(_file, resources);

                if (!themes.Any())
                {
                    await UIUtilities.ShowErrorDialogAsync("InvalidThemeFileTitle", "InvalidThemeFileNoThemesFound");
                    Window.Current.Close();

                    return;
                }

                Tools.InvertTheme(ActualTheme, this);
                this.Resources.MergedDictionaries.Add(resources);
                Tools.InvertTheme(ActualTheme, this);

                DataContext = themes.First().Value;
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("InvalidThemeFileTitle", ex.Message);
                Window.Current.Close();
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;

            try
            {
                var theme = await ThemeManager.InstallFromArchiveAsync(_file, true);
                if (EnableAfterInstallationCheckBox.IsChecked == true)
                {
                    var selectedThemeNames = App.LocalSettings.Read(SELECTED_THEME_NAMES, new List<string>());
                    selectedThemeNames.Add(theme.NormalisedName);

                    App.LocalSettings.Save(SELECTED_THEME_NAMES, selectedThemeNames);

                    await ThemeHelpers.RequestRestartAsync();
                }

                InstallButton.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Visible;

                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync("Failed to install theme!", ex.Message);
            }

            InstallButton.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.Current.Close();
        }
    }
}
