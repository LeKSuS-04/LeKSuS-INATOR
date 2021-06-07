using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace DSBot {
    [Hidden]
    [RequireUserPermissions(Permissions.Administrator)]
    partial class ModCommands : BaseCommandModule {
        [Command("clearchannel")]
        [Aliases("clear_channel", "clear-channel")]
        [Description("Deletes all messages that are less than 14 days old from this channel.")]
        public async Task ClearChannel(CommandContext ctx) {
            var messages = await ctx.Channel.GetMessagesAsync(1000000);
            await ctx.Channel.DeleteMessagesAsync(messages.Where(m => (DateTimeOffset.UtcNow - m.Timestamp).TotalDays <= 14));
        }

        [Command("sudo")]
        [Description("allows to execute command as another user.")]
        public async Task Sudo(CommandContext ctx, [Description("Member to execute as.")]  DiscordMember member, [RemainingText, Description("Command to execute.")] string commandString) {
            var commandsNext = ctx.CommandsNext;
            var command = commandsNext.FindCommand(commandString, out var customArgs);

            var fakeContext = commandsNext.CreateFakeContext(member, ctx.Channel, commandString, ctx.Prefix, command, customArgs);
            await commandsNext.ExecuteCommandAsync(fakeContext);
        }

        [Command("say")]
        [Description("Sends the message from bot's account into channel")]
        [Aliases("send", "type")]
        public async Task Say(CommandContext ctx, [Description("ID of the channel to send message to (or \"this\" to send in this channel)")] string channelId, [RemainingText, Description("Message to send")]string message) {
            await ctx.Message.DeleteAsync();

            DiscordChannel channel;
            if(new List<string> { "this", "here" }.Contains(channelId.ToLower())) {
                channel = ctx.Channel;
            } else {
                try {
                    channel = await ctx.Client.GetChannelAsync(Convert.ToUInt64(channelId));
                } catch {
                    await ctx.Channel.SendMessageAsync("Cannot find specified channel.");
                    return;
                }
            }
            
            await channel.TriggerTypingAsync();
            await channel.SendMessageAsync(message);
        }
    }
}
