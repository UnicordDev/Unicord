using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace DSharpPlus.EventArgs
{
    public class CallDeleteEventArgs : DiscordEventArgs
    {
        public CallDeleteEventArgs(DiscordClient client) : base(client)
        {
        }

        public DiscordDmChannel Channel { get; internal set; }
        public DiscordCall Call { get; internal set; }
    }
}
