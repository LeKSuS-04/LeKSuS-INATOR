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
    class HelpFormatter : BaseHelpFormatter {
        private StringBuilder MessageBuilder { get; }

        public HelpFormatter(CommandContext commandContext) : base(commandContext) {
            MessageBuilder = new StringBuilder();
        }

        public override BaseHelpFormatter WithCommand(Command command) {
            MessageBuilder.Append(Formatter.Bold("Command: "))
                          .AppendLine(Formatter.Underline(command.Name));
            
            if(command.Aliases.Count > 0) { 
                MessageBuilder.Append(Formatter.Bold("Also refered to as: "))
                              .AppendLine(string.Join(", ", command.Aliases));
            }
            if(!string.IsNullOrWhiteSpace(command.Description)) {
                MessageBuilder.Append(Formatter.Bold("Description: "))
                              .AppendLine(command.Description);
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands) {
            MessageBuilder.Append(Formatter.Bold("Try to use with: "))
                          .AppendLine(string.Join(", ", subcommands.Select(sc => sc.Name)));
            return this;
        }

        public override CommandHelpMessage Build() {
            return new CommandHelpMessage(MessageBuilder.ToString().Replace("\r\n", "\n"));
        }
    }
}
