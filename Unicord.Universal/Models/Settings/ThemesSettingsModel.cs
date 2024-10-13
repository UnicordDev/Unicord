using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using Unicord.Universal.Services;
using Windows.Storage;
using Windows.UI.Xaml;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    // not super useful considering most things here can't nicely be MMVMed
    public class ThemesSettingsModel : ViewModelBase
    {
        private bool _isDirty;
        private int _appTheme;

        private int _initialColour;
        private int _initialTheme;

        public ThemesSettingsModel()
        {
            _appTheme = (int)ThemeService.GetForCurrentView().GetSettingsTheme();
            _initialColour = App.LocalSettings.Read(REQUESTED_COLOUR_SCHEME, (int)ElementTheme.Default);
            _initialTheme = _appTheme;
        }


        public bool SunValleyThemeSupported
            => SystemInformation.Instance.OperatingSystemVersion.Build >= 17763;

        public bool IsLoading { get; internal set; }
        public ElementTheme PreviewRequestedTheme { get; private set; }

        public int ColourScheme
        {
            get => (int)App.LocalSettings.Read(REQUESTED_COLOUR_SCHEME, (int)ElementTheme.Default);
            set
            {
                App.LocalSettings.Save(REQUESTED_COLOUR_SCHEME, value);
                InvokePropertyChanged(nameof(ColourScheme));
                InvokePropertyChanged(nameof(IsDirty));
            }
        }

        public int ApplicationTheme
        {
            get => _appTheme;
            set
            {
                OnPropertySet(ref _appTheme, value);
                ThemeService.GetForCurrentView()
                    .SetThemeOnRelaunch((AppTheme)value);
                InvokePropertyChanged(nameof(IsDirty));
            }
        }

        public bool IsDirty
            => ApplicationTheme != _initialTheme || ColourScheme != _initialColour;
    }
}
