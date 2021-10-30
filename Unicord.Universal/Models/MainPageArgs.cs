using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.ViewManagement;

namespace Unicord.Universal.Models
{
    internal class MainPageArgs
    {
        public ulong ChannelId { get; internal set; }
        public bool IsUriActivation { get; internal set; }
        public ulong UserId { get; set; }
        public bool FullFrame { get; set; }
        public ApplicationViewMode ViewMode { get; internal set; }
        public SplashScreen SplashScreen { get; internal set; }
        public Exception ThemeLoadException { get; internal set; }
    }
}
