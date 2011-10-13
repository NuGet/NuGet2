using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet.Commands;

namespace NuGet
{
    public class Program
    {
        private const string NuGetExtensionsKey = "NUGET_EXTENSIONS_PATH";
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

        public static int Main(string[] args)
        {
            var console = new NuGet.Common.Console();
            var fileSystem = new PhysicalFileSystem(Directory.GetCurrentDirectory());
            try
            {
                // Remove NuGet.exe.old
                RemoveOldFile(fileSystem);

                // Import Dependencies  
                var p = new Program();
                p.Initialize(fileSystem);

                // Register an additional provider for the console specific application so that the user
                // will be prompted if a proxy is set and credentials are required
                HttpClient.DefaultCredentialProvider = new ConsoleCredentialProvider();

                // Add commands to the manager
                foreach (ICommand cmd in p.Commands)
                {
                    p.Manager.RegisterCommand(cmd);
                }

                CommandLineParser parser = new CommandLineParser(p.Manager);

                // Parse the command
                ICommand command = parser.ParseCommandLine(args) ?? p.HelpCommand;

                // Fallback on the help command if we failed to parse a valid command
                if (!ArgumentCountValid(command))
                {
                    // Get the command name and add it to the argument list of the help command
                    string commandName = command.CommandAttribute.CommandName;

                    // Print invalid command then show help
                    Console.WriteLine(NuGetResources.InvalidArguments, commandName);

                    p.HelpCommand.ViewHelpForCommand(commandName);
                }
                else
                {
                    command.Execute();
                }
            }
            catch (Exception e)
            {
                console.WriteError(e.Message);
                return 1;
            }
            return 0;
        }

        private void Initialize(IFileSystem fileSystem)
        {
            using (var catalog = new AggregateCatalog(new AssemblyCatalog(GetType().Assembly)))
            {
                if (!IgnoreExtensions)
                {
                    AddExtensionsToCatalog(catalog);
                }
                using (var container = new CompositionContainer(catalog))
                {
                    var settings = GetCommandLineSettings(fileSystem);
                    var defaultPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl);

                    var officialPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl, NuGetResources.OfficialPackageSourceName);
                    var v1PackageSource = new PackageSource(NuGetConstants.V1FeedUrl, NuGetResources.OfficialPackageSourceName);

                    var packageSourceProvider = new PackageSourceProvider(
                        settings,
                        new[] { defaultPackageSource },
                        new Dictionary<PackageSource, PackageSource> { 
                            { v1PackageSource, officialPackageSource }
                        }
                    );

                    container.ComposeExportedValue<ISettings>(settings);
                    container.ComposeExportedValue<IPackageRepositoryFactory>(new NuGet.Common.CommandLineRepositoryFactory());
                    container.ComposeExportedValue<IPackageSourceProvider>(packageSourceProvider);
                    container.ComposeParts(this);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want to block the exe from usage if anything failed")]
        internal static void RemoveOldFile(IFileSystem fileSystem)
        {
            string oldFile = typeof(Program).Assembly.Location + ".old";
            try
            {
                if (fileSystem.FileExists(oldFile))
                {
                    fileSystem.DeleteFile(oldFile);
                }
            }
            catch
            {
                // We don't want to block the exe from usage if anything failed
            }
        }

        public static bool ArgumentCountValid(ICommand command)
        {
            CommandAttribute attribute = command.CommandAttribute;
            return command.Arguments.Count >= attribute.MinArgs &&
                   command.Arguments.Count <= attribute.MaxArgs;
        }

        private static void AddExtensionsToCatalog(AggregateCatalog catalog)
        {
            IEnumerable<string> directories = new[] { ExtensionsDirectoryRoot };

            var customExtensions = Environment.GetEnvironmentVariable(NuGetExtensionsKey);
            if (!String.IsNullOrEmpty(customExtensions))
            {
                // Add all directories from the environment variable if available.
                directories = directories.Concat(customExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var directory in directories)
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
                    RegisterExtensions(catalog, files);
                }
            }
        }

        private static void RegisterExtensions(AggregateCatalog catalog, IEnumerable<string> enumerateFiles)
        {
            foreach (var item in enumerateFiles)
            {
                try
                {
                    catalog.Catalogs.Add(new AssemblyCatalog(item));
                }
                catch (BadImageFormatException)
                {
                    // Ignore if the dll wasn't a valid assembly
                }
            }
        }

        internal static ISettings GetCommandLineSettings(IFileSystem workingDirectory)
        {
            if (workingDirectory.FileExists(Constants.SettingsFileName))
            {
                return new Settings(workingDirectory);
            }
            return Settings.DefaultSettings;
        }
    }
}
