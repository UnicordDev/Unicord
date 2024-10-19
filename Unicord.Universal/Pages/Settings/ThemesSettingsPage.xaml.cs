using System;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class ThemesSettingsPage : Page, INotifyOnExit
    {
        private string _initialTheme;
        private int _initialColour;
        private bool _loaded;
        private bool _dragging;

        public ThemesSettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            OnClosing();
        }

        private void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ThemesSettingsModel)DataContext).ColourScheme = ((ComboBox)sender).SelectedIndex;
        }

        private void ApplicationThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ThemesSettingsModel)DataContext).ApplicationTheme = ((ComboBox)sender).SelectedIndex;
        }

        public async void OnClosing()
        {
            if (!((ThemesSettingsModel)DataContext).IsDirty)
                return;

            if (ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync"))
            {
                var resources = ResourceLoader.GetForCurrentView("ThemesSettingsPage");
                if (await UIUtilities.ShowYesNoDialogAsync(resources.GetString("ThemeChangedTitle"), resources.GetString("ThemeChangedMessage")))
                {
                    await CoreApplication.RequestRestartAsync("");
                }
            }
        }
    }
}
