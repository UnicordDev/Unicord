using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Models
{
    internal class MainPageEventArgs
    {
        internal ulong ChannelId { get; set; }
        internal bool FullFrame { get; set; }
    }
}
