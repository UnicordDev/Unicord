using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Models
{
    public class CreateServerModel : ViewModelBase
    {
        private string _name;
        private DiscordVoiceRegion _region;
        private ImageSource _icon;

        public CreateServerModel()
        {
            Region = Regions?.FirstOrDefault(r => r.IsOptimal);
        }

        public string Name { get => _name; set => OnPropertySet(ref _name, value); }
        public DiscordVoiceRegion Region { get => _region; set => OnPropertySet(ref _region, value); }
        public ImageSource Icon { get => _icon; set => OnPropertySet(ref _icon, value); }

        public StorageFile IconFile { get; set; }

        public IEnumerable<DiscordVoiceRegion> Regions
            => discord?.VoiceRegions.Values.Where(r => !r.IsDeprecated && !r.IsVIP).OrderBy(r => r.Name);
    }
}
