using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Common;
using System.ComponentModel.Composition;

namespace NuGet.Commands {
    public abstract class Command : ICommand {

        public List<string> Arguments { get; set; }

        [Import]
        public IConsole Console { get; set; }

        [Import("HelpCommand")]
        public HelpCommand HelpCommand { get; set; }

        [Option("help", AltName = "?")]
        public bool Help { get; set; }

        public void Execute() {
            if (Help) {
                HelpCommand.ViewHelpForCommand(GetCurrentCommandName());
            }
            else {
                ExecuteCommand();
            }
        }

        public string GetCurrentCommandName() {
            var CommandAttributes = this.GetType().GetCustomAttributes(typeof(CommandAttribute), true);
            if (CommandAttributes.Any()) {
                return ((CommandAttribute)CommandAttributes.First()).CommandName;
            }
            return String.Empty;
        }

        public abstract void ExecuteCommand();
    }
}
