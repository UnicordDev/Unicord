using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    // not super useful considering most things here can't nicely be MMVMed
    public class ThemesSettingsModel : NotifyPropertyChangeImpl
    {
        private bool _isDirty;

        public ThemesSettingsModel()
        {
            AvailableThemes = new ObservableCollection<Theme>();
            SelectedThemes = new ObservableCollection<Theme>();
            AvailableThemes.CollectionChanged += OnAvailableThemesUpdated;
        }

        private void OnAvailableThemesUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            App.LocalSettings.Save(AVAILABLE_THEME_NAMES, AvailableThemes.ToList().Select(t => t.NormalisedName));
        }

        public bool IsLoading { get; internal set; }
        public bool ShowThemesPlaceholder => !AvailableThemes.Any();
        public ElementTheme PreviewRequestedTheme { get; private set; }
        public ObservableCollection<Theme> AvailableThemes { get; private set; }
        public ObservableCollection<Theme> SelectedThemes { get; private set; }

        public int ColourScheme
        {
            get => (int)App.LocalSettings.Read(REQUESTED_COLOUR_SCHEME, ElementTheme.Default);
            set
            {
                IsDirty = true;
                App.LocalSettings.Save(REQUESTED_COLOUR_SCHEME, (ElementTheme)value);
                InvokePropertyChanged(nameof(ColourScheme));
            }
        }

        public bool IsDirty { get => _isDirty; internal set => OnPropertySet(ref _isDirty, value); }
    }
}
