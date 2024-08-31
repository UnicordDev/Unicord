using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
    public class StickerViewModel : ViewModelBase
    {
        private readonly DiscordMessageSticker sticker;

        public StickerViewModel(DiscordMessageSticker sticker, MessageViewModel parent)
            :base(parent)
        {
            this.sticker = sticker;
        }
    }
}
