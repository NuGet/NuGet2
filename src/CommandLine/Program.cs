using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NuGet.Commands;

namespace NuGet {
    public class Program {
        private static readonly string ExtensionsDirectoryRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "Commands");

        [Import]
        public HelpCommand HelpCommand { get; set; }

        [ImportMany]
        public IEnumerable<ICommand> Commands { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        /// <summary>
        /// Flag meant for unit tests that prevents command line extensions from being loaded.
        /// </summary>
        public static bool IgnoreExtensions { get; set; }

        public void Initialize() {
            using (var catalog = new AggregateCatalog(new AssemblyCatalog(GetType().Assembly))) {
                if (!IgnoreExtensions) {
                    AddExtensionsToCatalog(catalog);
                }
                using (var container = new CompositionContainer(catalog)) {
                    var defaultPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl);
                    var packageSourceProvider = new PackageSourceProvider(Settings.UserSettings, new[] { defaultPackageSource });

                    container.ComposeExportedValue<IPackageRepositoryFactory>(new NuGet.Common.CommandLineRepositoryFactory());
                    container.ComposeExportedValue<IPackageSourceProvider>(packageSourceProvider);
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

                // Register an additional provider for the console specific application so that the user
                // will be prompted if a proxy is set and credentials are required
                var consoleCredentialProvider = new ConsoleCredentialProvider();
                HttpClient.DefaultProxyFinder.RegisterProvider(consoleCredentialProvider);
                HttpClient.DefaultRequestCredentialService.RegisterProvider(consoleCredentialProvider);

                // Add commands to the manager
                foreach (ICommand cmd in p.Commands) {
                    p.Manager.RegisterCommand(cmd);
                }

                CommandLineParser parser = new CommandLineParser(p.Manager);

                // Parse the command
                ICommand command = parser.ParseCommandLine(args) ?? p.HelpCommand;

                // Fallback on the help command if we failed to parse a valid command
                if (!ArgumentCountValid(command)) {
                    // Get the command name and add it to the argumet list of the help command
                    string commandName = command.CommandAttribute.CommandName;

                    // Print invalid command then show help
                    Console.WriteLine(NuGetResources.InvalidArguments, commandName);

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want to block the exe from usage if anything failed")]
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

        public static bool ArgumentCountValid(ICommand command) {
            CommandAttribute attribute = command.CommandAttribute;
            return command.Arguments.Count >= attribute.MinArgs &&
                   command.Arguments.Count <= attribute.MaxArgs;
        }

        private static void AddExtensionsToCatalog(AggregateCatalog catalog) {
            if (!Directory.Exists(ExtensionsDirectoryRoot)) {
                return;
            }
            foreach (var item in Directory.EnumerateFiles(ExtensionsDirectoryRoot, "*.dll", SearchOption.AllDirectories)) {
                try {
                    catalog.Catalogs.Add(new AssemblyCatalog(item));
                }
                catch (BadImageFormatException) {
                    // Ignore if the dll wasn't a valid assembly
                }
            }
        }
    }
}