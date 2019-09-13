using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Unicord.Universal.Commands
{
    public class SlashCommands : BaseCommandModule
    {
        [Command("shrug")]
        public async Task ShrugAsync(CommandContext ctx, [RemainingText] string content)
        {
            await ctx.RespondAsync(content + "¯\\_(ツ)_/¯");
        }
    }
}
