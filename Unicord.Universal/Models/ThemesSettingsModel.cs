using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Unicord.Universal.Models
{
    // not super useful considering most things here can't nicely be MMVMed
    public class ThemesSettingsModel : PropertyChangedBase
    {
        private object _selectedTheme;
        private List<Theme> _availableThemes;

        public ThemesSettingsModel()
        {
            SelectedTheme = Theme.Default;
        }

        public async Task ReloadThemes()
        {
            var themesList = new List<Theme>();

            var selectedTheme = App.LocalSettings.Read("SelectedThemeName", string.Empty);
            if (string.IsNullOrWhiteSpace(selectedTheme))
                selectedTheme = "Default";

            themesList.Add(Theme.Default);

            var themeDirectory = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Themes", CreationCollisionOption.OpenIfExists);
            var directories = await themeDirectory.GetFoldersAsync();
            foreach (var directory in directories)
            {
                if (await directory.TryGetItemAsync("theme.json") is StorageFile themeJson)
                {
                    try
                    {
                        var theme = JsonConvert.DeserializeObject<Theme>(await FileIO.ReadTextAsync(themeJson));
                        themesList.Add(theme);
                    }
                    catch { }
                }
            }

            AvailableThemes = themesList;
            SelectedTheme = themesList.FirstOrDefault(t => t.Name == selectedTheme) ?? Theme.Default;
        }

        public ElementTheme PreviewRequestedTheme { get; set; }

        public List<Theme> AvailableThemes
        {
            get => _availableThemes;
            set
            {
                OnPropertySet(ref _availableThemes, value);
                InvokePropertyChanged(nameof(SelectedTheme));
                InvokePropertyChanged(nameof(CanRemove));
            }
        }

        public object SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                OnPropertySet(ref _selectedTheme, value);
                InvokePropertyChanged(nameof(CanRemove));
            }
        }

        public Visibility CanRemove => (SelectedTheme as Theme)?.IsDefault ?? true ? Visibility.Collapsed : Visibility.Visible;

        public int ColourScheme
        {
            get => (int)App.LocalSettings.Read("RequestedTheme", ElementTheme.Default);
            set
            {
                App.LocalSettings.Save("RequestedTheme", (ElementTheme)value);
                InvokePropertyChanged(nameof(ColourScheme));
            }
        }

        public double ScaleFactor
        {
            get => App.RoamingSettings.Read<double>("ScaleFactor", 1);
            set
            {
                App.RoamingSettings.Save("ScaleFactor", value);
                InvokePropertyChanged(nameof(ScaleFactorText));
            }
        }

        public string ScaleFactorText
        {
            get => $"{Math.Floor(ScaleFactor * 100)}% ";
        }
    }
}
