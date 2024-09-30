using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Unicord.Constants;

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
