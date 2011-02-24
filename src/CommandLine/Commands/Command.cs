using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands {
    public abstract class Command : ICommand {
        private const string CommandSuffix = "Command";
        private CommandAttribute _commandAttribute;

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

        public CommandAttribute CommandAttribute {
            get {
                if (_commandAttribute == null) {
                    _commandAttribute = GetCommandAttribute();
                }
                return _commandAttribute;
            }
        }

        public string GetCurrentCommandName() {
            return CommandAttribute.CommandName;
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

        public virtual CommandAttribute GetCommandAttribute() {
            var attributes = GetType().GetCustomAttributes(typeof(CommandAttribute), true);
            if (attributes.Any()) {
                return (CommandAttribute) attributes.FirstOrDefault();
            }

            // Use the command name minus the suffix if present and default description
            string name = GetType().Name;
            int idx = name.LastIndexOf(CommandSuffix, StringComparison.OrdinalIgnoreCase);
            if(idx >= 0){
                name = name.Substring(0, idx);
            }
            if (!String.IsNullOrEmpty(name)) {
                return new CommandAttribute(name, NuGetResources.DefaultCommandDescription);
            }
            return null;
        } 
    }
}
