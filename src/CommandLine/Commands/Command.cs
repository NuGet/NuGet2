using System.Collections.Generic;
using System.ComponentModel.Composition;
using NuGet.Common;

namespace NuGet.Commands {
    public abstract class Command : ICommand {
        public Command() {
            Arguments = new List<string>();
        }

        public List<string> Arguments { get; set; }

        [Import]
        public IConsole Console { get; set; }

        [Import]
        public HelpCommand HelpCommand { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        [Option("help", AltName = "?")]
        public bool Help { get; set; }


        public string GetCurrentCommandName() {
            return Manager.GetCommandAttribute(this).CommandName;
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
