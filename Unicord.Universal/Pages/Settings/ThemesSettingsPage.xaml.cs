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

                preview.RequestedTheme = (ElementTheme)model.ColourScheme;
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.LocalSettings.Save(SELECTED_THEME_NAMES, Model.SelectedThemes.OrderBy(t => Model.AvailableThemes.IndexOf(t)).Select(s => s.NormalisedName).Distinct().ToList());

            if (!Model.IsDirty)
                return;

            await ThemeHelpers.RequestRestartAsync();
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
            FixSelectedThemes();
        }

        private void FixSelectedThemes()
        {
            _loaded = false;

            themesList.SelectedItems.Clear();
            foreach (var item in Model.SelectedThemes)
            {
                themesList.SelectedItems.Add(item);
            }

            _loaded = true;
        }

        private async void AddThemesButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".uni-theme");
            picker.FileTypeFilter.Add(".zip");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ThemeHelpers.SafeInstallFromArchiveAsync(file);
            }

            await Model.ReloadThemes();
        }

        private async void ThemesList_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Any(i => { var ext = Path.GetExtension(i.Name); return ext == ".zip" || ext == ".uni-theme"; }))
                {
                    e.Handled = true;
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        private async void ThemesList_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var themeItems = items.OfType<StorageFile>().Where(i => { var ext = Path.GetExtension(i.Name); return ext == ".zip" || ext == ".uni-theme"; });
                foreach (var theme in themeItems)
                {
                    e.Handled = true;
                    e.AcceptedOperation = DataPackageOperation.Copy;

                    deferral?.Complete();
                    deferral = null;

                    await ThemeHelpers.SafeInstallFromArchiveAsync(theme);
                }

                await Model.ReloadThemes();
            }
        }

        private void ThemesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.IsLoading || !_loaded || _dragging)
                return;

            foreach (var item in e.AddedItems.OfType<Theme>())
            {
                Model.SelectedThemes.Add(item);
            }

            foreach (var item in e.RemovedItems.OfType<Theme>())
            {
                Model.SelectedThemes.Remove(item);
            }

            var names = Model.SelectedThemes.OrderBy(t => Model.AvailableThemes.IndexOf(t)).Select(s => s.NormalisedName).Reverse().Distinct().ToList();
            App.LocalSettings.Save(SELECTED_THEME_NAMES, names);

            Model.IsDirty = true;
            ReloadThemes(names, preview);
        }

        private void ReloadThemes(List<string> names, FrameworkElement el)
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
            {
                var dictionary = new ResourceDictionary();
                ThemeManager.Load(names, dictionary);

                var requestedTheme = (ElementTheme)Model.ColourScheme;
                Tools.InvertTheme(requestedTheme, preview);

                el.Resources = dictionary;
                el.RequestedTheme = (ElementTheme)Model.ColourScheme;
                el.InvalidateMeasure();
                el.InvalidateArrange();
            }
        }

        private void ThemesList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            _dragging = true;
        }

        private void ThemesList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            _dragging = false;
        }

        private async void Grid_Tapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;

            var item = (sender as FrameworkElement).DataContext as Theme;
            var themesDirectory = await ApplicationData.Current.LocalFolder.GetFolderAsync(THEME_FOLDER_NAME);
            var themeDirectory = await themesDirectory.GetFolderAsync(item.NormalisedName);
            item.DisplayLogoSource = new BitmapImage(new Uri(Path.Combine(themeDirectory.Path, item.DisplayLogo)));

            var manageDialog = new ManageThemeDialog() { DataContext = item };
            await manageDialog.ShowAsync();
            await Model.ReloadThemes();
        }
    }
}
