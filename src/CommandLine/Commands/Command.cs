using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    public abstract class Command : ICommand {

        public List<string> Arguments { get; set; }

        [Import]
        public IConsole Console { get; set; }

        [Import]
        public HelpCommand HelpCommand { get; set; }

        [Option("help", AltName = "?")]
        public bool Help { get; set; }

        public string GetCurrentCommandName() {
            var CommandAttribute = this.GetType()
                                        .GetCustomAttributes(typeof(CommandAttribute), true)
                                        .Cast<CommandAttribute>()
                                        .FirstOrDefault();
            if (CommandAttribute != null) {
                return CommandAttribute.CommandName;
            }
            return String.Empty;
        }

        public void Execute() {
            if (Help) {
                HelpCommand.ViewHelpForCommand(GetCurrentCommandName());
            }
            else {
                ExecuteCommand();
            }
        }

        public abstract void ExecuteCommand();
    }
}
