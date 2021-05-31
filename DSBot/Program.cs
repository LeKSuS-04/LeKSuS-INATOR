using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DSBot {
    class Program {
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
                MinimumLogLevel = LogLevel.Debug
            };

            BotClient = new DiscordClient(discordConfiguration);
            
            BotClient.Ready += BotClientReady;
            BotClient.GuildAvailable += BotClientGuildAvailable;
            BotClient.ClientErrored += BotClientError;

            CommandsNextConfiguration commandsConfiguration =  new CommandsNextConfiguration() {
                StringPrefixes = new[] { jsonConfig.CommandPrefix },
                EnableDefaultHelp = false,
                EnableDms = true,
                EnableMentionPrefix = true
            };

            Commands = BotClient.UseCommandsNext(commandsConfiguration);

            Commands.RegisterCommands<BaseCommands>();

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
    }

    public struct JsonConfig {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}