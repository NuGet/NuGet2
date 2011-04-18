using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using NuGet.Commands;

namespace NuGet {
    public class Program {
        [Import]
        public HelpCommand HelpCommand { get; set; }

        [ImportMany]
        public List<ICommand> Commands { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        public void Initialize() {
            using (AggregateCatalog catalog = new AggregateCatalog(new AssemblyCatalog(this.GetType().Assembly))) {
                using (var container = new CompositionContainer(catalog)) {
                    container.ComposeExportedValue<IPackageRepositoryFactory>(new NuGet.Common.CommandLineRepositoryFactory());
                    container.ComposeParts(this);
                }
            }
        }

        public static int Main(string[] args) {
            try {
                // Remove NuGet.exe.old
                RemoveOldFile();

                // Import Dependecies  
                var p = new Program();
                p.Initialize();


                // Add commands to the manager
                foreach (ICommand cmd in p.Commands) {
                    p.Manager.RegisterCommand(cmd);
                }

                CommandLineParser parser = new CommandLineParser(p.Manager);

                // Parse the command
                ICommand command = parser.ParseCommandLine(args) ?? p.HelpCommand;

                // Fallback on the help command if we failed to parse a valid command
                if (!p.ArgumentCountValid(command)) {
                    // Get the command name and add it to the argumet list of the help command
                    string commandName = command.CommandAttribute.CommandName;

                    // Print invalid command then show help
                    Console.WriteLine(NuGet.Common.NuGetResources.InvalidArguments, commandName);

                    p.HelpCommand.ViewHelpForCommand(commandName);
                }
                else {
                    command.Execute();
                }
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

        private static void RemoveOldFile() {
            string oldFile = typeof(Program).Assembly.Location + ".old";
            try {
                if (File.Exists(oldFile)) {
                    File.Delete(oldFile);
                }
            }
            catch {
                // We don't want to block the exe from usage if anything failed
            }
        }

        public bool ArgumentCountValid(ICommand command) {
            CommandAttribute attribute = command.CommandAttribute;
            return command.Arguments.Count >= attribute.MinArgs &&
                   command.Arguments.Count <= attribute.MaxArgs;
        }

    }
}
