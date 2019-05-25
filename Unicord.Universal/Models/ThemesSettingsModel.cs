using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Windows.UI.Xaml;

namespace Unicord.Universal.Models
{
    // not super useful considering most things here can't nicely be MMVMed
    public class ThemesSettingsModel : PropertyChangedBase
    {
        private object _selectedTheme;

        public ThemesSettingsModel()
        {
            var defaultTheme = new Theme() { Name = "Default", Author = "N/A", IsDefault = true };
            var selectedTheme = App.LocalSettings.Read("SelectedTheme", defaultTheme);
            var installedThemes = App.LocalSettings.Read("InstalledThemes", new Dictionary<string, Theme>());

            AvailableThemes.Add(defaultTheme);
            AvailableThemes.AddRange(installedThemes.Values);

            SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Name == selectedTheme.Name);
        }

        public ElementTheme PreviewRequestedTheme { get; set; }

        public List<Theme> AvailableThemes { get; set; } = new List<Theme>();

        public object SelectedTheme { get => _selectedTheme; set => OnPropertySet(ref _selectedTheme, value); }
    }
}
