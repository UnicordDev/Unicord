using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Models
{
    internal class RecentServerModel
    {
        public RecentServerModel()
        {
            ChannelMessagesSent = new Dictionary<ulong, int>();            
        }

        public int MessageSent { get; set; }

        public Dictionary<ulong, int> ChannelMessagesSent { get; set; }
    }
}
