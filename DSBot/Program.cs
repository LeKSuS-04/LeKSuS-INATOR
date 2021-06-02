using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DSBot {
    class Program {
        public Random Rng { private get; set; }
        public readonly EventId BotEventId = new EventId(42, "LeKSuS-INATOR");
        public DiscordClient BotClient { get; set; }
        public CommandsNextExtension Commands { get; set; }

        static void Main(string[] args) {
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync() {
            string json = "";
            using (FileStream fs = File.OpenRead("config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            var jsonConfig = JsonConvert.DeserializeObject<JsonConfig>(json);
            var discordConfiguration = new DiscordConfiguration {
                Token = jsonConfig.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };

            BotClient = new DiscordClient(discordConfiguration);
            BotClient.Ready += BotClientReady;
            BotClient.GuildAvailable += BotClientGuildAvailable;
            BotClient.ClientErrored += BotClientError;

            var services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider();

            var commandsConfiguration =  new CommandsNextConfiguration() {
                StringPrefixes = new[] { jsonConfig.CommandPrefix },
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = services
            };

            Commands = BotClient.UseCommandsNext(commandsConfiguration);
            Commands.CommandExecuted += CommandExecuted;
            Commands.CommandErrored += CommandError;

            Commands.RegisterCommands<BaseCommands>();
            Commands.RegisterCommands<ModCommands>();
            Commands.RegisterCommands<HangmanCommands>();

            Commands.SetHelpFormatter<HelpFormatter>();

            await BotClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task BotClientReady(DiscordClient sender, ReadyEventArgs e) {
            sender.Logger.LogInformation(BotEventId, $"Discord bot client ({BotEventId.Name}) is up and ready to work!");
            return Task.CompletedTask;
        }
        private Task BotClientGuildAvailable(DiscordClient sender, GuildCreateEventArgs e) {
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");
            return Task.CompletedTask;
        }
        private Task BotClientError(DiscordClient sender, ClientErrorEventArgs e) {
            sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");
            return Task.CompletedTask;
        }

        private Task CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e) {
            e.Context.Client.Logger.LogInformation(BotEventId, $"{e.Context.User.Username} successfully executed \"{e.Command.QualifiedName}\" commmand with {(e.Context.RawArguments.Count > 0 ? $"\"{e.Context.RawArgumentString}\"" : "no")} arguments");
            return Task.CompletedTask;
        }
        private async Task CommandError(CommandsNextExtension sender, CommandErrorEventArgs e) {
            e.Context.Client.Logger.LogError(BotEventId, $"{e.Context.User.Username} tried executing \"{e.Command?.QualifiedName ?? "<unknown>"}\" command, but failed due to the {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if(e.Exception is ChecksFailedException ex) {
                DiscordEmoji emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions to execute this command.",
                    Color = new DiscordColor(0xFF0000)
                };
                await e.Context.RespondAsync(embed);
            }
        }
    }

    public struct JsonConfig {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}