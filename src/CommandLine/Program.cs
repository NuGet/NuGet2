using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace NuGet {
    public class Program {
        private HelpCommand _helpCommand;
        public HelpCommand HelpCommand {
            get {
                if (_helpCommand == null) {
                    _helpCommand = Commands.OfType<HelpCommand>().Single();
                }
                return _helpCommand;
            }
        }

        [ImportMany]
        public List<ICommand> Commands { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        public void Initialize() {
            using (AggregateCatalog catalog = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()))) {
                using (var container = new CompositionContainer(catalog)) {
                    container.ComposeExportedValue<IPackageRepositoryFactory>(PackageRepositoryFactory.Default);
                    container.ComposeParts(this);
                }
            }
        }

        public static int Main(string[] args) {
            // Import Dependecies  
            var p = new Program();
            p.Initialize();

            // Add commands to the manager
            foreach (ICommand command in p.Commands) {
                p.Manager.RegisterCommand(command);
            }

            CommandLineParser parser = new CommandLineParser(p.Manager);

            try {
                // Parse the command
                ICommand command = parser.ParseCommandLine(Environment.CommandLine);

                // Fallback on the help command if we failed to parse a valid command
                if (command == null) {
                    command = p.HelpCommand;
                }
                else if (!p.ArgumentCountValid(command)) {
                    // If the argument count isn't valid then show help for the command
                    ICommand helpCommand = p.HelpCommand;

                    // Get the command name and add it to the argumet list of the help command
                    string commandName = p.Manager.GetCommandAttribute(command).CommandName;
                    helpCommand.Arguments = new List<string>();
                    helpCommand.Arguments.Add(commandName);
                    command = helpCommand;

                    // Print invalid command then show help
                    Console.WriteLine(NuGet.Common.NuGetResources.InvalidArguments, commandName);
                }

                command.Execute();
            }
            catch (Exception e) {
                var currentColor = ConsoleColor.Gray;
                try {
                    currentColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(e.Message);
                }
                finally {
                    Console.ForegroundColor = currentColor;
                }
                return 1;
            }
            return 0;
        }

        public bool ArgumentCountValid(ICommand command) {
            CommandAttribute attribute = Manager.GetCommandAttribute(command);
            return command.Arguments.Count >= attribute.MinArgs &&
                   command.Arguments.Count <= attribute.MaxArgs;
        }

    }
}
