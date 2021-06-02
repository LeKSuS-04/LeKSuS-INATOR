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
        public static readonly string[] pictures = {
            "  +---+\n  |   |\n  O   |\n /|\\  |\n / \\  |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|\\  |\n /    |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|\\  |\n      |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n /|   |\n      |\n      |\n=========", 
            "  +---+\n  |   |\n  O   |\n  |   |\n      |\n      |\n=========",
            "  +---+\n  |   |\n  O   |\n      |\n      |\n      |\n=========",
            "  +---+\n  |   |\n      |\n      |\n      |\n      |\n=========",
            " \n      |\n      |\n      |\n      |\n      |\n=========",
            " \n \n \n \n \n \n========="
        };
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
        private Dictionary<ulong, HangmanGame> games = new Dictionary<ulong, HangmanGame>();
        private string GameInfo(HangmanGame game, bool HideWord = true) {
            StringBuilder output = new StringBuilder();

            output.AppendLine(HangmanGame.pictures[game.livesLeft])
                  .AppendLine($"Lives: {game.livesLeft}/{HangmanGame.pictures.Length - 1}")
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
            await ctx.TriggerTypingAsync();

            if(games.ContainsKey(ctx.User.Id)) {
                await ctx.Channel.SendMessageAsync($"@{ctx.Member?.Mention ?? ctx.User.Mention}, you have already started the game. If you wanna surrender, type \"!hangman surrender\" before starting new game. If you don't remember, what's going on in that game, \"!hangman game\" can remind you that");
            } else {
                var hangmanGame = new HangmanGame();

                hangmanGame.livesLeft = HangmanGame.pictures.Length - 1;
                hangmanGame.lettersTried = new List<char>();
                hangmanGame.word = HangmanGame.wordlist[Rng.Next(0, HangmanGame.wordlist.Count)];

                games.Add(ctx.User.Id, hangmanGame);

                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, the game has started!\n{GameInfo(games[ctx.User.Id])}");
            }
        }

        [Command("game")]
        [Aliases("info", "information", "state")]
        [Description("Returns information about current game.")]
        public async Task Info(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            if(games.ContainsKey(ctx.User.Id)) {
                await ctx.Channel.SendMessageAsync(GameInfo(games[ctx.User.Id]));
            } else {
                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you don't have any started games. Wanna play? Type \"!hangman play\"");
            }
        }

        [Command("guess")]
        [Aliases("try", "check")]
        [Description("Check if the letter is in the word!")]
        public async Task Guess(CommandContext ctx, [Description("Letter to check")] string letterString) {
            await ctx.TriggerTypingAsync();

            if(games.ContainsKey(ctx.User.Id)) {
                if(letterString.Length > 1) {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you only can try one letter at a time!");
                } else {
                    char letter = letterString.ToLower()[0];

                    if(letter >= 'a' && letter <= 'z') {
                        if(games[ctx.User.Id].lettersTried.Contains(letter)) {
                            await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you've already tried letter \"{letter}\", see?\n{GameInfo(games[ctx.User.Id])}");
                        } else {
                            games[ctx.User.Id].AddLetter(letter);

                            if(games[ctx.User.Id].word.Contains(letter)) {
                                if(games[ctx.User.Id].word.Contains(HangmanGame.hidingChar)) {
                                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, yay, letter \"{letter}\" is in the word!\n{GameInfo(games[ctx.User.Id])}");
                                } else {
                                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, yay, letter \"{letter}\" is in the word!\nYou've guessed the word! You win!!!\n{GameInfo(games[ctx.User.Id])}\nStart new game by typing \"!hangman play\"");
                                    games.Remove(ctx.User.Id);
                                }
                            } else {
                                games[ctx.User.Id].livesLeft -= 1;

                                if(games[ctx.User.Id].livesLeft > 0) {
                                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, nope, letter \"{letter}\" is not in the word!\n{GameInfo(games[ctx.User.Id])}");
                                } else {
                                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, nope, letter \"{letter}\" is not in the word!\nYou have no lives left! You lose...\n{GameInfo(games[ctx.User.Id], false)}\nStart new game by typing \"!hangman play\"");
                                    games.Remove(ctx.User.Id);
                                }
                            }
                        }
                    } else { 
                        await ctx.Channel.SendMessageAsync($"Hey {ctx.Member?.Mention ?? ctx.User.Mention}, \"{letter}\" is not a letter!");
                    }
                }
            } else {
                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you don't have any started games. Wanna play? Type \"!hangman play\"");
            }
        }

        [Command("word")]
        [Aliases("guessword", "guess-word", "guess_word", "tryword", "try-word", "try_word", "fullword", "full-word", "full_word", "answer")]
        [Description("If you know the word why won't you say it instead of guessig letter-by-letter?")]
        public async Task Word(CommandContext ctx, [Description("Your guess")] string word) {
            await ctx.TriggerTypingAsync();

            word = word.Trim().ToLower();
            if(games.ContainsKey(ctx.User.Id)) {
                bool hasForeignSymbols = false;
                char foreignChar = '*';
                foreach(char letter in word) {
                    if(letter < 'a' || letter > 'z') {
                        foreignChar = letter;
                        hasForeignSymbols = true;
                        break;
                    }
                }

                if(hasForeignSymbols) {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, your word has characters that are not letters, for example \"{foreignChar}\"");
                } else { 
                    if(word == games[ctx.User.Id].getSecretWord) {
                        await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, yay, you're right\nYou've guessed the word! You win!!!\n{GameInfo(games[ctx.User.Id], false)}\nStart new game by typing \"!hangman play\"");
                        games.Remove(ctx.User.Id);
                    } else {
                        games[ctx.User.Id].livesLeft -= 1;

                        if(games[ctx.User.Id].livesLeft > 0) {
                            await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, nope, that's not the word! You lose a life.\n{GameInfo(games[ctx.User.Id])}");
                        } else {
                            await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, nope, that's not the word! You lose a life.\nYou have no lives left! You lose...\n{GameInfo(games[ctx.User.Id], false)}\nStart new game by typing \"!hangman play\"");
                            games.Remove(ctx.User.Id);
                        }
                    }
                }
            } else {
                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you don't have any started games. Wanna play? Type \"!hangman play\"");
            }
        }

        [Command("surrender")]
        [Aliases("giveup", "give-up", "give_up", "lose", "end")]
        [Description("Ends the current game (for loosers)")]
        public async Task Surrender(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            if(games.ContainsKey(ctx.User.Id)) {
                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, your game was ended.\n{GameInfo(games[ctx.User.Id], false)}");
                games.Remove(ctx.User.Id);

                var L  = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_l:");
                var O  = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_o:");
                var O2 = DiscordEmoji.FromName(ctx.Client, ":o2:");
                var S  = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_s:");
                var E  = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_e:");
                var R  = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_r:");

                await ctx.Message.CreateReactionAsync(L);
                await ctx.Message.CreateReactionAsync(O);
                await ctx.Message.CreateReactionAsync(O2);
                await ctx.Message.CreateReactionAsync(S);
                await ctx.Message.CreateReactionAsync(E);
                await ctx.Message.CreateReactionAsync(R);
            } else {
                await ctx.Channel.SendMessageAsync($"{ctx.Member?.Mention ?? ctx.User.Mention}, you don't have any started games. How can you surrender?");
            }
        }
    }
}
