using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DSBot {
    class BaseCommands : BaseCommandModule {
        [Command("ping")]
        [Description("Used to check bot's ping.")]
        public async Task Ping(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            await ctx.RespondAsync($"Pong! Ping is {ctx.Client.Ping}ms");

            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");
            await ctx.Message.CreateReactionAsync(emoji);
        }
    }
}
