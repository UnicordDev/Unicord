using System;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.ViewManagement;

namespace Unicord.Universal.Models
{
    internal class MainPageArgs
    {
        public ulong ChannelId { get; internal set; }
        public bool IsUriActivation { get; internal set; }
        public ulong UserId { get; set; }
        public bool FullFrame { get; set; }
        public SplashScreen SplashScreen { get; internal set; }
        public ShareOperation ShareOperation { get; internal set; }
    }
}
