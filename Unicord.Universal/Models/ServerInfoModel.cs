using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models
{
    public class ServerInfoModel
    {
        public DiscordGuild Server { get; set; }

        public async Task LoadAsync()
        {

        }
    }
}
