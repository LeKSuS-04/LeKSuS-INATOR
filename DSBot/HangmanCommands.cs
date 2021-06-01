using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext.Attributes;

namespace DSBot {
    class HangmanGame {
        public const char hidingChar = '_';
        public const int livesTotal = 6;
        public static List<string> wordlist = File.ReadAllText(Directory.GetCurrentDirectory() + "\\wordlist.txt").Split("\r\n").ToList();
        public int livesLeft { get; set; }
        private string secretWord;
        public List<char> lettersTried;

        public bool isWordGuessed {
            get {
                foreach(char c in word) {
                    if(!lettersTried.Contains(c)) return false;
                }
                return true;
            }
        }
        public string word {
            get { 
                StringBuilder hiddenWord = new StringBuilder(secretWord.Length);

                for(int i = 0; i < secretWord.Length; i++) {
                    if(lettersTried.Contains(secretWord[i])) hiddenWord.Append(secretWord[i]);
                    else hiddenWord.Append(hidingChar);
                }

                return hiddenWord.ToString();
            }
            set {
                secretWord = value.ToLower();
            }
        }
        public string getSecretWord { get { return secretWord; } }

        public void AddLetter(char c) {
            if(!lettersTried.Contains(c)) lettersTried.Add(c);
            lettersTried.Sort();
        }
    }

    [Group("hangman")]
    [Aliases("hm")]
    [Description("Classic hangman game. Try to guess the word!")]
    class HangmanCommands : BaseCommandModule {
        public Random Rng { private get; set; }
        private Dictionary<DiscordUser, HangmanGame> games = new Dictionary<DiscordUser, HangmanGame>();
        static string[] hangmanPics = {
            "  +---+\n  |   |\n  O   |\n /|\\  |\n / \\  |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|\\  |\n /    |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|\\  |\n      |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|   |\n      |\n      |\n=========", 
            "  +---+\n  |   |\n  O   |\n  |   |\n      |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n      |\n      |\n      |\n=========",
            "  +---+\n  |   |\n      |\n      |\n      |\n      |\n=========" 
        };
        private string GameInfo(HangmanGame game, bool HideWord = true) {
            StringBuilder output = new StringBuilder();

            output.AppendLine(hangmanPics[game.livesLeft])
                  .AppendLine($"Lives: {game.livesLeft}/{HangmanGame.livesTotal}")
                  .AppendLine($"Word: {(HideWord ? game.word : game.getSecretWord)}")
                  .AppendLine($"Tried letters: {string.Join(", ", game.lettersTried)}");

            return Formatter.BlockCode(output.ToString().Replace("\r\n", "\n"));
        } 

        [Command("rules")]
        [Aliases("howtoplay", "how-to-play", "how_to_play", "help")]
        [Description("Official rules for hangman game.")]
        public async Task Rules(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder {
                Title = "Hangman rules",
                Description = $"Aight, this one is simple.\n" +
                              $"I pick a word, you're trying to guess it one letter at a time.\n" +
                              $"If the letter's in, you get to see where it is. If not, you lose a life.\n" +
                              $"Your goal is to guess the word before running out of lifes\n" +
                              $"Let's play?",
                Color = new DiscordColor(0xe74c3c)
            };

            await ctx.Channel.SendMessageAsync(embed);
        }

        [Command("play")]
        [Aliases("newgame", "new-game", "new_game", "start", "restart", "go", "begin")]
        [Description("Starts a new hangman game.")]
        public async Task Play(CommandContext ctx) {
            if(games.ContainsKey(ctx.User)) {
                await ctx.TriggerTypingAsync();
                await ctx.Channel.SendMessageAsync($"@{ctx.Member.Mention ?? ctx.User.Mention}, you have already started the game. If you wanna surrender, type \"hangman surrender\" before starting new game. If you don't remember, what's going on in that game, \"hangman game\" can remind you that");
            } else {
                var hangmanGame = new HangmanGame();

                hangmanGame.livesLeft = HangmanGame.livesTotal;
                hangmanGame.lettersTried = new List<char>();
                hangmanGame.word = HangmanGame.wordlist[Rng.Next(0, HangmanGame.wordlist.Count)];

                games.Add(ctx.User, hangmanGame);

                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, the game has started!\n{GameInfo(games[ctx.User])}");
            }
        }

        [Command("game")]
        [Aliases("info", "information", "state")]
        [Description("Returns information about current game.")]
        public async Task Info(CommandContext ctx) {
            if(games.ContainsKey(ctx.User)) {
                await ctx.Channel.SendMessageAsync(GameInfo(games[ctx.User]));
            } else {
                await ctx.TriggerTypingAsync();
                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, you don't have any started games. Wanna play? Type \"hangman play\"");
            }
        }

        [Command("guess")]
        [Aliases("try", "check")]
        [Description("Check if the letter is in the word!")]
        public async Task Guess(CommandContext ctx, [Description("Letter to check")] string letterString) {
            if(games.ContainsKey(ctx.User)) {
                if(letterString.Length > 1) {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, you only can try one letter at a time!");
                } else {
                    char letter = letterString[0];
                    if(games[ctx.User].lettersTried.Contains(letter)) {
                        await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, you've already tried this letter, see?\n{GameInfo(games[ctx.User])}");
                    } else {
                        games[ctx.User].AddLetter(letter);

                        if(games[ctx.User].word.Contains(letter)) {
                            if(games[ctx.User].word.Contains(HangmanGame.hidingChar)) {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, yay, this letter is in the word!\n{GameInfo(games[ctx.User])}");
                            } else {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, yay, this letter is in the word!You've guessed the word! You win!!!\n{GameInfo(games[ctx.User])}\nStart new game by typing \"hangman play\"");
                                games.Remove(ctx.User);
                            }
                        } else {
                            games[ctx.User].livesLeft -= 1;

                            if(games[ctx.User].livesLeft > 0) {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, nope, this letter is not in the word!\n{GameInfo(games[ctx.User])}");
                            } else {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, nope, this letter is not in the word!\nYou have no lives left! You lose...\n{GameInfo(games[ctx.User], false)}\nStart new game by typing \"hangman play\"");
                                games.Remove(ctx.User);
                            }
                        }
                    }
                }
            } else {
                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention ?? ctx.User.Mention}, you don't have any started games. Wanna play? Type \"hangman play\"");
            }
        }
    }
}
