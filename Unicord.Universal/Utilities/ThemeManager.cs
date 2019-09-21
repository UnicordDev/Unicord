using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal
{
    internal static class ThemeManager
    {
        public static void LoadCurrentTheme(ResourceDictionary dictionary)
        {
            var theme = App.LocalSettings.Read("SelectedThemeName", string.Empty);
            if (!string.IsNullOrWhiteSpace(theme))
            {
                Load(theme, dictionary);
            }
            else
            {
                dictionary.MergedDictionaries.Insert(0, new XamlControlsResources());
            }
        }

        public static void Load(string selectedThemeName, ResourceDictionary target)
        {
            // this code must be synchronous to prevent any race conditions that may occur when 
            // loading assets at startup. if this code was asynchronous, UWP can sometimes jump ahead and 
            // assume startup has completed before we've fully loaded our assets, which means some changes
            // made by themes may not load properly. (missing colours, templates, etc.)

            // this also means, however, that we cannot call this method after the app is activated, 
            // because synchronous I/O is not allowed on the UI thread, and this code must assume
            // it's running on the UI thread, because otherwise it would be asynchronous... bleh

            var resources = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var selectedTheme = Theme.Default;

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var themesFolderPath = Path.Combine(localFolder.Path, "Themes");

                if (!Directory.Exists(themesFolderPath))
                    Directory.CreateDirectory(themesFolderPath);

                var themePath = Path.Combine(themesFolderPath, Strings.Normalise(selectedThemeName));
                if (!Directory.Exists(themePath))
                {
                    App.LocalSettings.Save("SelectedThemeName", string.Empty);
                    throw new Exception(resources.GetString("ThemeInvalidDoesNotExist"));
                }

                selectedTheme = JsonConvert.DeserializeObject<Theme>(File.ReadAllText(Path.Combine(themePath, "theme.json")));

                foreach (var file in Directory.EnumerateFiles(themePath, "*.xaml"))
                {
                    var text = File.ReadAllText(file);
                    var obj = XamlReader.Load(text);
                    if (obj is ResourceDictionary dictionary)
                    {
                        target.MergedDictionaries.Add(dictionary);
                    }
                }
            }
            finally
            {
                // ensure XamlControlsResources gets loaded even if theme loading itself fails
                target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = selectedTheme?.UseCompact ?? false });
            }
        }

        public static async Task LoadAsync(string themeName, ResourceDictionary target)
        {
            var resources = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var selectedTheme = Theme.Default;

            try
            {
                var localFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);
                if (!(await localFolder.TryGetItemAsync(Strings.Normalise(themeName)) is StorageFolder themeFolder) ||
                    !(await themeFolder.TryGetItemAsync("theme.json") is StorageFile themeDefinitionFile))
                {
                    throw new Exception(resources.GetString("ThemeInvalidDoesNotExist"));
                }

                selectedTheme = JsonConvert.DeserializeObject<Theme>(await FileIO.ReadTextAsync(themeDefinitionFile));

                foreach (var file in (await themeFolder.GetFilesAsync()).Where(f => Path.GetExtension(f.Name) == ".xaml"))
                {
                    Exception exception = null;
                    var text = await FileIO.ReadTextAsync(file);
                    await target.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        try
                        {
                            var obj = XamlReader.Load(text);
                            if (obj is ResourceDictionary dictionary)
                            {
                                target.MergedDictionaries.Add(dictionary);
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    });

                    if (exception != null)
                        throw exception;
                }
            }
            finally
            {
                // ensure XamlControlsResources gets loaded even if theme loading itself fails
                target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = selectedTheme?.UseCompact ?? false });
            }
        }

        public static async Task RemoveThemeAsync(string name)
        {
            if (name == string.Empty)
                return;

            var selectedThemeName = App.LocalSettings.Read("SelectedThemeName", string.Empty);
            if (selectedThemeName == name)
            {
                App.LocalSettings.Save("SelectedThemeName", string.Empty);
            }

            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);

            if (await themesDirectory.TryGetItemAsync(Strings.Normalise(name)) is StorageFolder folder)
                await folder.DeleteAsync();
        }

        /// <summary>
        /// Installs a theme from a file, validating the archive in the process.
        /// Will throw a lot if the archive is invalid.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task InstallFromFileAsync(StorageFile file)
        {
            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);
            var resources = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");

            // open the archive
            using (var fileStream = await file.OpenReadAsync())
            using (var archive = await Task.Run(() => new ZipArchive(fileStream.AsStreamForRead(), ZipArchiveMode.Read, true)))
            {
                // ensure it has a theme definition and load it
                Theme theme = null;
                var themeDefinitionFile = archive.GetEntry("theme.json");
                if (themeDefinitionFile == null)
                    throw new InvalidOperationException(resources.GetString("ThemeInvalidNoJson"));

                using (var reader = new StreamReader(themeDefinitionFile.Open()))
                    theme = JsonConvert.DeserializeObject<Theme>(await reader.ReadToEndAsync());

                if (await themesDirectory.TryGetItemAsync(theme.NormalisedName) is StorageFolder directory)
                {
                    if (await UIUtilities.ShowYesNoDialogAsync(resources.GetString("ThemeAlreadyInstalledTitle"), resources.GetString("ThemeAlreadyInstalledMessage")))
                    {
                        await directory.DeleteAsync();
                    }
                    else
                    {
                        return;
                    }
                }

                foreach (var check in theme.ContractChecks)
                {
                    if (!ApiInformation.IsApiContractPresent(check.Key, check.Value))
                        throw new InvalidOperationException(
                            check.Key == "Windows.Foundation.UniversalApiContract" ?
                            resources.GetString("ThemeUnsupportedWindowsVersion") :
                            resources.GetString("ThemePreInstallCheckFailed"));
                }

                // if the theme specifies a logo
                if (theme.DisplayLogo != null)
                {
                    await LoadDisplayLogo(archive, theme);
                }

                var dialog = new InstallThemeDialog(theme);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    var themeRoot = await themesDirectory.CreateFolderAsync(theme.NormalisedName);
                    await Task.Run(() => archive.ExtractToDirectory(themeRoot.Path, true));
                }
            }
        }

        private static async Task LoadDisplayLogo(ZipArchive archive, Theme theme)
        {
            // try and find it in the archive
            var resources = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var entry = archive.GetEntry(theme.DisplayLogo);
            if (entry == null)
                throw new InvalidOperationException(resources.GetString("ThemeInvalidNoLogo"));

            // then load it into an image source            
            using (var stream = new InMemoryRandomAccessStream())
            {
                var image = new BitmapImage();
                using (var entryStream = entry.Open())
                {
                    var destination = stream.AsStreamForWrite();
                    await entryStream.CopyToAsync(destination);
                    await destination.FlushAsync();
                }

                stream.Seek(0);
                await image.SetSourceAsync(stream);
                theme.DisplayLogoSource = image;
            }
        }
    }

    public class Theme
    {
        public static Theme Default
            => new Theme() { Name = "Default", Author = "N/A", IsDefault = true };

        public bool IsDefault { get; set; } = false;

        // internal properties used in databinding
        [JsonIgnore]
        public ImageSource DisplayLogoSource { get; set; }

        [JsonIgnore]
        public Brush DisplayColourBrush =>
           DisplayColour != default ? new SolidColorBrush(Color.FromArgb(DisplayColour.A, DisplayColour.R, DisplayColour.G, DisplayColour.B)) : null;

        [JsonIgnore]
        public string NormalisedName =>
            Strings.Normalise(Name);

        /// <summary>
        /// The short display name of your theme
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// A short promotional block of text describing your theme
        /// </summary>
        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        /// <summary>
        /// A longer promotional block of text describing your theme, used to describe
        /// your theme on the download page.
        /// </summary>
        [JsonProperty("long_description")]
        public string LongDescription { get; set; }

        /// <summary>
        /// Your name for credit
        /// </summary>
        [JsonProperty("author", Required = Required.Always)]
        public string Author { get; set; }

        /// <summary>
        /// The accent colour for your theme, used behind <see cref="DisplayLogo"/>
        /// </summary>
        [JsonProperty("display_colour")]
        public System.Drawing.Color DisplayColour { get; set; }

        /// <summary>
        /// A path to a logo for your theme, relative to the root of 
        /// the archive. e.g. "assets/logo.png"
        /// </summary>
        [JsonProperty("display_logo")]
        public string DisplayLogo { get; set; }

        /// <summary>
        /// Use compact resources?
        /// </summary>
        [JsonProperty("use_compact")]
        public bool UseCompact { get; set; }

        /// <summary>
        /// Provides a list of API Contracts to check for before loading a theme
        /// </summary>
        [JsonProperty("contract_checks", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ushort> ContractChecks { get; set; }
            = new Dictionary<string, ushort>();

    }
}
