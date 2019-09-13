using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Unicord.Universal.Commands
{
    public class SlashCommands : BaseCommandModule
    {
        [Command("shrug")]
        [IncludeAttachments]
        public async Task ShrugAsync(CommandContext ctx, [RemainingText] string content)
        {
            await ctx.RespondAsync(content + " ¯\\_(ツ)_/¯");
        }

        [Command("mute")]
        public Task MuteAsync(CommandContext ctx, [RemainingText] string content)
        {
            ctx.Channel.Muted = !ctx.Channel.Muted;
            ctx.RespondWithSystemMessage($"{ctx.Channel.Mention} has been {(ctx.Channel.Muted ? "muted" : "unmuted")}!");
            return Task.CompletedTask;
        }

        [Command("nick")]
        [RequireUserPermissions(Permissions.ChangeNickname)]
        public async Task NicknameAsync(CommandContext ctx, string nickname)
        {
            if (nickname.Length > 32)
                ctx.RespondWithSystemMessage("That nickname is too long!");

            await ctx.Member.ModifyAsync(m => m.Nickname = nickname);
            ctx.RespondWithSystemMessage($"You are now known as {nickname}!");
        }

        [Command("nick")]
        [RequireUserPermissions(Permissions.ManageNicknames)]
        public async Task NicknameAsync(CommandContext ctx, DiscordMember user, string nickname)
        {
            if (nickname.Length > 32)
                ctx.RespondWithSystemMessage("That nickname is too long!");

            await user.ModifyAsync(m => m.Nickname = nickname);
            ctx.RespondWithSystemMessage($"@{user.Username}#{user.Discriminator} is now known as {nickname}!");
        }

        [Command("spoiler")]
        [IncludeAttachments]
        public async Task SpoilerAsync(CommandContext ctx, [RemainingText] string content)
        {
            await ctx.RespondAsync($"|| {content.Trim()} ||");
        }

        [Command("me")]
        [IncludeAttachments]
        public async Task MeAsync(CommandContext ctx, [RemainingText] string content)
        {
            await ctx.RespondAsync($"*{content.Trim()}*");
        }
    }
}
