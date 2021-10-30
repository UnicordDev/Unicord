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
    public sealed partial class ThemesSettingsPage : Page
    {
        private string _initialTheme;
        private int _initialColour;
        private bool _loaded;
        private bool _dragging;

        public ThemesSettingsModel Model { get; }

        public ThemesSettingsPage()
        {
            Model = new ThemesSettingsModel();
            Model.PropertyChanged += ThemesSettingsPage_PropertyChanged;

            InitializeComponent();
            DataContext = Model;

            _initialColour = (int)App.LocalSettings.Read(REQUESTED_COLOUR_SCHEME, ElementTheme.Default);
        }

        private void ThemesSettingsPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ThemesSettingsModel model)
            {
                if (model.ColourScheme != _initialColour)
                {
                    model.IsDirty = true;
                }
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (!Model.IsDirty)
                return;

            await ThemeHelpers.RequestRestartAsync();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }       
    }
}
