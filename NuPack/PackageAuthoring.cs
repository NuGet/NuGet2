namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Reflection;

    public class PackageAuthoring {
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
                    container.ComposeParts(this);
                }
            }

        }

        public static int Main(string[] args) {
            // Import Dependecies  
            var p = new PackageAuthoring();
            p.Initialize();

            // Add commands to the manager
            foreach (ICommand command in p.Commands) {
                p.Manager.RegisterCommand(command);
            }

            CommandLineParser parser = new CommandLineParser(p.Manager);
            try {
                // Parse the command and fallback on the help command
                ICommand parsedCommand = parser.ParseCommandLine(Environment.CommandLine) ?? p.HelpCommand;
                parsedCommand.Execute();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 1;
            }
            return 0;
        }
    }
}
