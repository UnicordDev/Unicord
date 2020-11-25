using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
using static Unicord.Constants;

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Unicord.Universal
{
    public class ThemeInstalledEventArgs : EventArgs
    {
        public Theme NewTheme { get; set; }
    }

    public class ThemeUpdatedEventArgs : EventArgs
    {
        public IReadOnlyList<Theme> NewThemes { get; set; }
    }

    internal static class ThemeManager
    {

        private static ThemePostProcessor _postprocessor
            = new ThemePostProcessor();

        /// <summary>
        /// Event fired whenever a new theme is installed.
        /// </summary>
        public static event EventHandler<ThemeInstalledEventArgs> ThemeInstalled;

        /// <summary>
        /// Event fired whenever the current theme set is changed.
        /// </summary>
        public static event EventHandler<ThemeUpdatedEventArgs> ThemesUpdated;

        public static void LoadCurrentTheme(ResourceDictionary dictionary)
        {
            var themesDictionary = new ResourceDictionary();

            try
            {
                var themes = App.LocalSettings.Read(SELECTED_THEME_NAMES, new List<string>());
                Load(themes, themesDictionary);
                Analytics.TrackEvent("Themes_Loaded", new Dictionary<string, string>() { ["SelectedThemes"] = JsonConvert.SerializeObject(themes) });

            }
            finally
            {
                dictionary.MergedDictionaries.Add(themesDictionary);
            }
        }

        public static void Load(List<string> selectedThemeNames, ResourceDictionary target)
        {
            // this code must be synchronous to prevent any race conditions that may occur when 
            // loading assets at startup. if this code was asynchronous, UWP can sometimes jump ahead and 
            // assume startup has completed before we've fully loaded our assets, which means some changes
            // made by themes may not load properly. (missing colours, templates, etc.)

            // this also means, however, that we cannot call this method after the app is activated, 
            // because synchronous I/O is not allowed on the UI thread, and this code must assume
            // it's running on the UI thread, because otherwise it would be asynchronous... bleh
            
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var compact = false;
            var themes = new List<Theme>();

            try
            {
                var exceptions = new List<Exception>();
                var localFolder = ApplicationData.Current.LocalFolder;
                var themesFolderPath = Path.Combine(localFolder.Path, THEME_FOLDER_NAME);

                if (!Directory.Exists(themesFolderPath))
                    Directory.CreateDirectory(themesFolderPath);

                foreach (var selectedThemeName in selectedThemeNames.AsEnumerable())
                {
                    try
                    {
                        var themeDictionary = new ResourceDictionary();
                        var themePath = Path.Combine(themesFolderPath, Strings.Normalise(selectedThemeName));

                        Logger.Log(themePath);

                        if (!Directory.Exists(themePath))
                        {
                            selectedThemeNames.Remove(selectedThemeName);
                            App.LocalSettings.Save(SELECTED_THEME_NAMES, selectedThemeNames);
                            Analytics.TrackEvent("Themes_LoadError", new Dictionary<string, string>() { ["Info"] = "ThemeInvalid" });
                            throw new Exception(strings.GetString("ThemeInvalidDoesNotExist"));
                        }

                        var theme = JsonConvert.DeserializeObject<Theme>(File.ReadAllText(Path.Combine(themePath, THEME_METADATA_NAME)));
                        compact = compact ? true : theme.UseCompact;
                        themes.Add(theme);

                        foreach (var file in Directory.EnumerateFiles(themePath, "*.xaml"))
                        {
                            var text = File.ReadAllText(file);
                            var obj = XamlReader.Load(text);
                            if (obj is ResourceDictionary dictionary)
                            {
                                themeDictionary.MergedDictionaries.Add(dictionary);
                            }
                        }

                        _postprocessor.PostProcessDictionary(theme, themeDictionary);

                        target.MergedDictionaries.Add(themeDictionary);
                    }
                    catch (Exception ex)
                    {
                        Analytics.TrackEvent("Themes_LoadError", new Dictionary<string, string>() { ["LoadException"] = ex.Message, ["Theme"] = selectedThemeName });
                        exceptions.Add(ex);
                    }
                }

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions);
                }
            }
            finally
            {
                // ensure XamlControlsResources gets loaded even if theme loading itself fails
                target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = compact });
            }

            ThemesUpdated?.Invoke(null, new ThemeUpdatedEventArgs() { NewThemes = themes });
        }

        public static async Task LoadAsync(List<string> themeNames, ResourceDictionary target)
        {
            Theme selectedTheme = null;
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var localFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(THEME_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            var compact = false;
            var themes = new List<Theme>();

            try
            {
                foreach (var themeName in themeNames.AsEnumerable())
                {
                    if (!(await localFolder.TryGetItemAsync(Strings.Normalise(themeName)) is StorageFolder themeFolder) ||
                        !(await themeFolder.TryGetItemAsync(THEME_METADATA_NAME) is StorageFile themeDefinitionFile))
                    {
                        Analytics.TrackEvent("Themes_LoadError", new Dictionary<string, string>() { ["Info"] = "ThemeInvalid" });
                        throw new Exception(strings.GetString("ThemeInvalidDoesNotExist"));
                    }

                    selectedTheme = JsonConvert.DeserializeObject<Theme>(await FileIO.ReadTextAsync(themeDefinitionFile));
                    compact = compact ? true : selectedTheme.UseCompact;
                    themes.Add(selectedTheme);

                    var themeErrors = new List<ThemeLoadError>();
                    foreach (var file in (await themeFolder.GetFilesAsync()).Where(f => Path.GetExtension(f.Name) == ".xaml"))
                    {
                        var text = await FileIO.ReadTextAsync(file);
                        await LoadXamlAsync(target, themeErrors, file.Name, text, file.Path);
                    }

                    if (themeErrors.Any())
                    {
                        Analytics.TrackEvent("Themes_LoadError", new Dictionary<string, string>() { ["Info"] = "XamlParseError", });
                        throw new InvalidOperationException(StringFromThemeErrors(themeErrors));
                    }
                }
            }
            finally
            {
                // ensure XamlControlsResources gets loaded even if theme loading itself fails
                target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = compact });
            }

            Analytics.TrackEvent("Themes_LoadedAsync", new Dictionary<string, string>() { ["SelectedThemes"] = JsonConvert.SerializeObject(themeNames) });
        }

        public static async Task<Dictionary<StorageFile, Theme>> LoadFromArchivesAsync(List<StorageFile> files, ResourceDictionary target)
        {
            var themes = new Dictionary<StorageFile, Theme>();
            var exceptions = new List<Exception>();

            try
            {
                foreach (var file in files)
                {
                    try
                    {
                        var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync(THEME_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
                        var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");

                        // open the archive
                        using (var fileStream = await file.OpenReadAsync())
                        using (var archive = await Task.Run(() => new ZipArchive(fileStream.AsStreamForRead(), ZipArchiveMode.Read, true)))
                        {
                            // ensure it has a theme definition and load it
                            var theme = await LoadArchiveThemeDefinitionAsync(archive);
                            if (theme.DisplayLogo != null)
                            {
                                await LoadDisplayLogoAsync(archive, theme);
                            }

                            await ValidateAndLoadArchiveAsync(archive, theme, target);
                            themes.Add(file, theme);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                if (exceptions.Any())
                    throw new AggregateException(exceptions);
            }
            finally
            {
                // ensure XamlControlsResources gets loaded even if theme loading itself fails
                target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = themes.Values.Any(t => t.UseCompact) /* TODO */ });
            }

            return themes;
        }

        /// <summary>
        /// Installs a theme from a file, validating the archive in the process.
        /// Will throw a lot if the archive is invalid.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<Theme> InstallFromArchiveAsync(StorageFile file)
        {
            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync(THEME_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");

            // open the archive
            using (var fileStream = await file.OpenReadAsync())
            using (var archive = await Task.Run(() => new ZipArchive(fileStream.AsStreamForRead(), ZipArchiveMode.Read, true)))
            {
                // ensure it has a theme definition and load it
                var theme = await LoadArchiveThemeDefinitionAsync(archive);

                if (await themesDirectory.TryGetItemAsync(theme.NormalisedName) is StorageFolder directory)
                {
                    if (await UIUtilities.ShowYesNoDialogAsync(strings.GetString("ThemeAlreadyInstalledTitle"), strings.GetString("ThemeAlreadyInstalledMessage")))
                    {
                        await directory.DeleteAsync();
                    }
                    else
                    {
                        return null;
                    }
                }

                // if the theme specifies a logo
                if (theme.DisplayLogo != null)
                {
                    await LoadDisplayLogoAsync(archive, theme);
                }

                var dialog = new InstallThemeDialog(theme);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await ValidateAndLoadArchiveAsync(archive, theme, null);

                    var themeRoot = await themesDirectory.CreateFolderAsync(theme.NormalisedName);
                    await Task.Run(() => archive.ExtractToDirectory(themeRoot.Path));
                    Analytics.TrackEvent("Themes_Installed", new Dictionary<string, string>() { ["Theme"] = JsonConvert.SerializeObject(theme.Name) });
                    return theme;
                }
            }

            return null;
        }

        /// <summary>
        /// Uninstalls a theme
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task RemoveThemeAsync(string name)
        {
            if (name == string.Empty)
                return;

            var selectedThemes = App.LocalSettings.Read<List<string>>(SELECTED_THEME_NAMES);
            if (selectedThemes != null && (selectedThemes.Contains(name) || selectedThemes.Contains(Strings.Normalise(name))))
            {
                selectedThemes.Remove(name);
                selectedThemes.Remove(Strings.Normalise(name));
                App.LocalSettings.Save(SELECTED_THEME_NAMES, selectedThemes);
            }

            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync(THEME_FOLDER_NAME, CreationCollisionOption.OpenIfExists);

            if (await themesDirectory.TryGetItemAsync(Strings.Normalise(name)) is StorageFolder folder)
                await folder.DeleteAsync();

            Analytics.TrackEvent("Themes_Removed", new Dictionary<string, string>() { ["Theme"] = JsonConvert.SerializeObject(name) });
        }

        /// <summary>
        /// Runs all validation checks and asynchronously loads theme resources from an archive.
        /// </summary>
        /// <param name="archive">The theme archive</param>
        /// <param name="theme">The archive's theme definition</param>
        /// <returns></returns>
        private static async Task ValidateAndLoadArchiveAsync(ZipArchive archive, Theme theme, ResourceDictionary dictionary)
        {
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            theme = theme ?? await LoadArchiveThemeDefinitionAsync(archive);

            foreach (var check in theme.ContractChecks)
            {
                if (!ApiInformation.IsApiContractPresent(check.Key, check.Value))
                {
                    Analytics.TrackEvent("Themes_InstallFailure", new Dictionary<string, string>() { ["Info"] = "ThemeCheckFailed" });

                    throw new InvalidOperationException(
                        check.Key == "Windows.Foundation.UniversalApiContract" ?
                        strings.GetString("ThemeUnsupportedWindowsVersion") :
                        strings.GetString("ThemePreInstallCheckFailed"));
                }
            }

            var themeErrors = new List<ThemeLoadError>();
            foreach (var entry in archive.Entries.Where(e => Path.GetExtension(e.FullName) == ".xaml"))
            {
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                {
                    var text = await reader.ReadToEndAsync();
                    await LoadXamlAsync(dictionary, themeErrors, entry.FullName, text);
                }
            }

            if (themeErrors.Any())
            {
                Analytics.TrackEvent("Themes_InstallFailure", new Dictionary<string, string>() { ["Info"] = "XamlParseError" });
                throw new InvalidOperationException(StringFromThemeErrors(themeErrors));
            }
        }


        private static async Task<Theme> LoadArchiveThemeDefinitionAsync(ZipArchive archive)
        {
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");

            Theme theme = null;
            var themeDefinitionFile = archive.GetEntry(THEME_METADATA_NAME);
            if (themeDefinitionFile == null)
            {
                Analytics.TrackEvent("Themes_InstallFailure", new Dictionary<string, string>() { ["Info"] = "ThemeInvalid" });
                throw new InvalidOperationException(strings.GetString("ThemeInvalidNoJson"));
            }

            using (var reader = new StreamReader(themeDefinitionFile.Open()))
                theme = JsonConvert.DeserializeObject<Theme>(await reader.ReadToEndAsync());
            return theme;
        }

        private static async Task LoadDisplayLogoAsync(ZipArchive archive, Theme theme)
        {
            // try and find it in the archive
            var strings = ResourceLoader.GetForViewIndependentUse("ThemesSettingsPage");
            var entry = archive.GetEntry(theme.DisplayLogo);
            if (entry == null)
            {
                Analytics.TrackEvent("Themes_InstallFailure", new Dictionary<string, string>() { ["Info"] = "InvalidLogo" });
                throw new InvalidOperationException(strings.GetString("ThemeInvalidNoLogo"));
            }

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

        private static async Task LoadXamlAsync(ResourceDictionary target, List<ThemeLoadError> themeErrors, string name, string text, string path = null)
        {
            var error = new ThemeLoadError() { FileName = name };
            try
            {
                var obj = target == null ? XamlReader.Load(text) : await target.Dispatcher.AwaitableRunAsync(() => XamlReader.Load(text));
                if (obj is ResourceDictionary dictionary)
                {
                    if (target != null)
                    {
                        await target.Dispatcher.AwaitableRunAsync(() =>
                        {
                            target.MergedDictionaries.Add(dictionary);
                        });
                    }
                }
                else
                {
                    error.Message = $"XAML object not of type ResourceDictionary";
                    themeErrors.Add(error);
                }
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                themeErrors.Add(error);
            }
        }

        private static string StringFromThemeErrors(List<ThemeLoadError> themeErrors) =>
            $"This theme contains errors and can't be loaded.\r\n" +
            $"{string.Join("\r\n", themeErrors.Select(t => $" - {t.FileName}: {t.Message}"))}";
    }

    public class ThemeLoadError
    {
        public string FileName { get; set; }
        public string Message { get; set; }
    }

    public class Theme
    {
        public bool IsDefault { get; set; } = false;

        // internal properties used in databinding
        [JsonIgnore]
        public ImageSource DisplayLogoSource { get; set; }

        [JsonIgnore]
        public Brush DisplayColourBrush =>
           DisplayColour != default ? new SolidColorBrush(Color.FromArgb(DisplayColour.A, DisplayColour.R, DisplayColour.G, DisplayColour.B)) : (Brush)App.Current.Resources["SystemControlBackgroundAccentBrush"];

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
        /// the archive. e.g. "/assets/logo.png"
        /// </summary>
        [JsonProperty("display_logo")]
        public string DisplayLogo { get; set; }

        /// <summary>
        /// Use compact resources?
        /// </summary>
        [JsonProperty("use_compact")]
        public bool UseCompact { get; set; }

        /// <summary>
        /// Enforce a specific theme?
        /// </summary>
        [JsonProperty("enforce_theme")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ElementTheme EnforcedTheme { get; set; } = ElementTheme.Default;

        /// <summary>
        /// Provides a list of API Contracts to check for before loading a theme
        /// </summary>
        [JsonProperty("contract_checks", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ushort> ContractChecks { get; set; }
            = new Dictionary<string, ushort>();

    }
}

#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.