using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Unicord.Universal.Models
{
    // not super useful considering most things here can't nicely be MMVMed
    public class ThemesSettingsModel : PropertyChangedBase
    {
        private object _selectedTheme;

        public ThemesSettingsModel()
        {
            ReloadThemes();
        }

        public void ReloadThemes()
        {
            AvailableThemes.Clear();

            var defaultTheme = Theme.Default;
            var selectedTheme = App.LocalSettings.Read("SelectedTheme", defaultTheme);
            var installedThemes = App.LocalSettings.Read("InstalledThemes", new Dictionary<string, Theme>());

            AvailableThemes.Add(defaultTheme);
            AvailableThemes.AddRange(installedThemes.Values);

            InvokePropertyChanged(nameof(AvailableThemes));

            SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Name == selectedTheme?.Name) ?? Theme.Default;
        }

        public ElementTheme PreviewRequestedTheme { get; set; }

        public List<Theme> AvailableThemes { get; set; } = new List<Theme>();

        public object SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                OnPropertySet(ref _selectedTheme, value);
                InvokePropertyChanged(nameof(CanRemove));
            }
        }

        public Visibility CanRemove => (SelectedTheme as Theme).IsDefault ? Visibility.Collapsed : Visibility.Visible;

        public int ColourScheme
        {
            get => (int)App.LocalSettings.Read("RequestedTheme", ElementTheme.Default);
            set
            {
                App.LocalSettings.Save("RequestedTheme", (ElementTheme)value);
                InvokePropertyChanged(nameof(ColourScheme));
            }
        }
    }
}
