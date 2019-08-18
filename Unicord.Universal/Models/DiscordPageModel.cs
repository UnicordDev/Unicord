using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Voice;

namespace Unicord.Universal.Models
{
    // TODO: Move functionaliy from DiscordPage.xaml.cs into this class
    class DiscordPageModel : PropertyChangedBase
    {
        private VoiceConnectionModel _voiceModel;
        private DiscordUser _currentUser;

        public DiscordPageModel()
        {
            CurrentUser = App.Discord.CurrentUser;
        }

        public DiscordUser CurrentUser { get => _currentUser; set => OnPropertySet(ref _currentUser, value); }
        public VoiceConnectionModel VoiceModel { get => _voiceModel; set => OnPropertySet(ref _voiceModel, value); }
    }
}
