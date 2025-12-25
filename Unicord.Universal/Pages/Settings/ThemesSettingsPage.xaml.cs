using System;
using System.Threading.Tasks;
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
        private bool _restartPromptShown;

        public ThemesSettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await OnClosingAsync();
        }

        private async void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var previousIndex = e.RemovedItems.Count > 0 ? ((ComboBox)sender).Items.IndexOf(e.RemovedItems[0]) : comboBox.SelectedIndex;
            
            ((ThemesSettingsModel)DataContext).ColourScheme = comboBox.SelectedIndex;
            
            if (!await OnClosingAsync())
            {
                // User clicked Cancel, revert the selection
                comboBox.SelectionChanged -= ColorSchemeComboBox_SelectionChanged;
                comboBox.SelectedIndex = previousIndex;
                ((ThemesSettingsModel)DataContext).ColourScheme = previousIndex;
                comboBox.SelectionChanged += ColorSchemeComboBox_SelectionChanged;
            }
        }

        private async void ApplicationThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var previousIndex = e.RemovedItems.Count > 0 ? ((ComboBox)sender).Items.IndexOf(e.RemovedItems[0]) : comboBox.SelectedIndex;
            
            ((ThemesSettingsModel)DataContext).ApplicationTheme = comboBox.SelectedIndex;
            
            if (!await OnClosingAsync())
            {
                // User clicked Cancel, revert the selection
                comboBox.SelectionChanged -= ApplicationThemeComboBox_SelectionChanged;
                comboBox.SelectedIndex = previousIndex;
                ((ThemesSettingsModel)DataContext).ApplicationTheme = previousIndex;
                comboBox.SelectionChanged += ApplicationThemeComboBox_SelectionChanged;
            }
        }

        public async Task<bool> OnClosingAsync()
        {
            if (!((ThemesSettingsModel)DataContext).IsDirty || _restartPromptShown)
                return true;

            if (ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync"))
            {
                _restartPromptShown = true;
                var resources = ResourceLoader.GetForCurrentView("ThemesSettingsPage");
                if (await UIUtilities.ShowYesNoDialogAsync(resources.GetString("ThemeChangedTitle"), resources.GetString("ThemeChangedMessage")))
                {
                    await CoreApplication.RequestRestartAsync("");
                    return true;
                }
                else
                {
                    _restartPromptShown = false;
                    return false;
                }
            }
            return true;
        }

        public async void OnClosing()
        {
            await OnClosingAsync();
        }
    }
}
