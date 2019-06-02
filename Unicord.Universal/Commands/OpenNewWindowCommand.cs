using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace Unicord.Universal.Commands
{
    class OpenNewWindowCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return (parameter is DiscordChannel) && AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop";
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordChannel channel)
            {
                await WindowManager.OpenChannelWindowAsync(channel);
            }
        }
    }
}
