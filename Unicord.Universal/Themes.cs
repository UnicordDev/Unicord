using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Utilities;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Search;
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
    internal static class Themes
    {
        public static async Task LoadAsync(Theme selectedTheme, ResourceDictionary target)
        {
            target.MergedDictionaries.Insert(0, new XamlControlsResources() { UseCompactResources = selectedTheme.UseCompact });

            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);
            var themeDictionary = await themesDirectory.CreateFolderAsync(selectedTheme.Name, CreationCollisionOption.OpenIfExists);
            var files = await themeDictionary.GetFilesAsync();
            foreach (var file in files.Where(f => Path.GetExtension(f.Name) == ".xaml"))
            {
                var text = await FileIO.ReadTextAsync(file);
                var obj = XamlReader.Load(text);
                if (obj is ResourceDictionary dictionary)
                {
                    target.MergedDictionaries.Add(dictionary);
                }
            }
        }

        /// <summary>
        /// Installs a theme from a file, validating the archive in the process.
        /// Will throw a lot if the archive is invalid.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task InstallFromFileAsync(StorageFile file)
        {
            var installedThemes = App.LocalSettings.Read("InstalledThemes", new Dictionary<string, Theme>());
            var themesDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);

            try
            {
                // open the archive
                using (var fileStream = await file.OpenReadAsync())
                using (var archive = await Task.Run(() => new ZipArchive(fileStream.AsStreamForRead(), ZipArchiveMode.Read, true)))
                {
                    // ensure it has a theme definition and load it
                    Theme theme = null;
                    var themeDefinitionFile = archive.GetEntry("theme.json");
                    if (themeDefinitionFile == null)
                        throw new InvalidOperationException("This file does not appear to be a Unicord theme, missing theme.json!");

                    using (var reader = new StreamReader(themeDefinitionFile.Open()))
                        theme = JsonConvert.DeserializeObject<Theme>(await reader.ReadToEndAsync());

                    if (installedThemes.ContainsKey(theme.Name))
                    {
                        if (await UIUtilities.ShowYesNoDialogAsync("Theme already installed!", "This theme is already installed, do you want to overwrite it?"))
                        {
                            var directory = await themesDirectory.GetFolderAsync(theme.Name);
                            if (directory != null)
                                await directory.DeleteAsync();

                            installedThemes.Remove(theme.Name);
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
                                "The version of Windows you're running is not supported by this theme. Sorry!" :
                                "A pre-install check failed! This probably means your device or the version of Windows you're running is not supported by this theme. Sorry!");
                    }

                    // if the theme specifies a logo
                    if (theme.DisplayLogo != null)
                    {
                        await LoadDisplayLogo(archive, theme);
                    }

                    var dialog = new InstallThemeDialog(theme);
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        var themeRoot = await themesDirectory.CreateFolderAsync(theme.Name);
                        await Task.Run(() => archive.ExtractToDirectory(themeRoot.Path, true));

                        installedThemes.Add(theme.Name, theme);
                    }
                }
            }
            finally
            {
                App.LocalSettings.Save("InstalledThemes", (object)installedThemes);
            }
        }

        private static async Task LoadDisplayLogo(ZipArchive archive, Theme theme)
        {
            // try and find it in the archive
            var entry = archive.GetEntry(theme.DisplayLogo);
            if (entry == null)
                throw new InvalidOperationException("This theme specifies a logo file that doesn't exist!");

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
        [JsonIgnore]
        public bool IsDefault { get; set; } = false;

        // internal properties used in databinding
        [JsonIgnore]
        public ImageSource DisplayLogoSource { get; set; }

        [JsonIgnore]
        public Brush DisplayColourBrush =>
           DisplayColour != default ? new SolidColorBrush(Color.FromArgb(DisplayColour.A, DisplayColour.R, DisplayColour.G, DisplayColour.B)) : null;

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
