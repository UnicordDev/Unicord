using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace DSharpPlus.EventArgs
{
    public class CallUpdateEventArgs : DiscordEventArgs
    {
        public CallUpdateEventArgs(DiscordClient client) : base(client)
        {
        }

        public DiscordDmChannel Channel { get; internal set; }
        public DiscordCall CallBefore { get; internal set; }
        public DiscordCall CallAfter { get; internal set; }
    }
}
