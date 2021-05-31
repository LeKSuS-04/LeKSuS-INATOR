using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DSBot {
    class BaseCommands : BaseCommandModule {
        [Command("Hello")]
        [Aliases("Hi")]
        public async Task Hello(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync($"Hello, {ctx.Member.DisplayName} :)");
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");
            await ctx.RespondAsync($"Pong! Ping is {ctx.Client.Ping}ms");
            await ctx.Message.CreateReactionAsync(emoji);
        }
    }
}
