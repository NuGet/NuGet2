using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;

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
            var console = new Common.Console();
            var fileSystem = new PhysicalFileSystem(Directory.GetCurrentDirectory());
            try
            {
                // Remove NuGet.exe.old
                RemoveOldFile(fileSystem);

                // Import Dependencies  
                var p = new Program();
                p.Initialize(fileSystem, console);

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
                    console.WriteLine(NuGetResources.InvalidArguments, commandName);

                    p.HelpCommand.ViewHelpForCommand(commandName);
                }
                else
                {
                    SetConsoleInteractivity(console, command as Command);
                    command.Execute();
                }
            }
            catch (AggregateException exception)
            {
                string message;
                Exception unwrappedEx = ExceptionUtility.Unwrap(exception);
                if (unwrappedEx == exception)
                {
                    // If the AggregateException contains more than one InnerException, it cannot be unwrapped. In which case, simply print out individual error messages
                    message = String.Join(Environment.NewLine, exception.InnerExceptions.Select(ex => ex.Message).Distinct(StringComparer.CurrentCulture));
                }
                else
                {
                    message = ExceptionUtility.Unwrap(exception).Message;
                }
                console.WriteError(message);
                return 1;
            }
            catch (Exception e)
            {
                console.WriteError(ExceptionUtility.Unwrap(e).Message);
                return 1;
            }
            return 0;
        }

        private void Initialize(IFileSystem fileSystem, IConsole console)
        {
            using (var catalog = new AggregateCatalog(new AssemblyCatalog(GetType().Assembly)))
            {
                if (!IgnoreExtensions)
                {
                    AddExtensionsToCatalog(catalog, console);
                }
                using (var container = new CompositionContainer(catalog))
                {
                    var settings = Settings.LoadDefaultSettings(fileSystem);
                    var defaultPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl);

                    var officialPackageSource = new PackageSource(NuGetConstants.DefaultFeedUrl, NuGetResources.OfficialPackageSourceName);
                    var v1PackageSource = new PackageSource(NuGetConstants.V1FeedUrl, NuGetResources.OfficialPackageSourceName);
                    var legacyV2PackageSource = new PackageSource(NuGetConstants.V2LegacyFeedUrl, NuGetResources.OfficialPackageSourceName);

                    var packageSourceProvider = new PackageSourceProvider(
                        settings,
                        new[] { defaultPackageSource },
                        new Dictionary<PackageSource, PackageSource> { 
                            { v1PackageSource, officialPackageSource },
                            { legacyV2PackageSource, officialPackageSource }
                        }
                    );

                    // Register an additional provider for the console specific application so that the user
                    // will be prompted if a proxy is set and credentials are required
                    var credentialProvider = new SettingsCredentialProvider(new ConsoleCredentialProvider(console), packageSourceProvider, console);
                    HttpClient.DefaultCredentialProvider = credentialProvider;

                    container.ComposeExportedValue<IConsole>(console);
                    container.ComposeExportedValue<ISettings>(settings);
                    container.ComposeExportedValue<IPackageRepositoryFactory>(new NuGet.Common.CommandLineRepositoryFactory());
                    container.ComposeExportedValue<IPackageSourceProvider>(packageSourceProvider);
                    container.ComposeExportedValue<ICredentialProvider>(credentialProvider);
                    container.ComposeExportedValue<IFileSystem>(fileSystem);
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

        private static void AddExtensionsToCatalog(AggregateCatalog catalog, IConsole console)
        {
            IEnumerable<string> directories = new[] { ExtensionsDirectoryRoot };

            var customExtensions = Environment.GetEnvironmentVariable(NuGetExtensionsKey);
            if (!String.IsNullOrEmpty(customExtensions))
            {
                // Add all directories from the environment variable if available.
                directories = directories.Concat(customExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            IEnumerable<string> files;
            foreach (var directory in directories)
            {
                if (Directory.Exists(directory))
                {
                    files = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
                    RegisterExtensions(catalog, files, console);
                }
            }

            // Ideally we want to look for all files. However, using MEF to identify imports results in assemblies being loaded and locked by our App Domain
            // which could be slow, might affect people's build systems and among other things breaks our build. 
            // Consequently, we'll use a convention - only binaries ending in the name Extensions would be loaded. 
            var nugetDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            files = Directory.EnumerateFiles(nugetDirectory, "*Extensions.dll");
            RegisterExtensions(catalog, files, console);
        }

        private static void RegisterExtensions(AggregateCatalog catalog, IEnumerable<string> enumerateFiles, IConsole console)
        {
            foreach (var item in enumerateFiles)
            {
                try
                {
                    catalog.Catalogs.Add(new AssemblyCatalog(item));
                }
                catch (BadImageFormatException ex)
                {
                    // Ignore if the dll wasn't a valid assembly
                    console.WriteWarning(ex.Message);
                }
                catch (FileLoadException ex)
                {
                    // Ignore if we couldn't load the assembly.
                    console.WriteWarning(ex.Message);
                }
            }
        }

        private static void SetConsoleInteractivity(IConsole console, Command command)
        {
            // Global environment variable to prevent the exe for prompting for credentials
            string globalSwitch = Environment.GetEnvironmentVariable("NUGET_EXE_NO_PROMPT");

            // When running from inside VS, no input is available to our executable locking up VS.
            // VS sets up a couple of environment variables one of which is named VisualStudioVersion. 
            // Every time this is setup, we will just fail.
            // TODO: Remove this in next iteration. This is meant for short-term backwards compat.
            string vsSwitch = Environment.GetEnvironmentVariable("VisualStudioVersion");

            console.IsNonInteractive = !String.IsNullOrEmpty(globalSwitch) ||
                                       !String.IsNullOrEmpty(vsSwitch) ||
                                       (command != null && command.NonInteractive);

            if (command != null)
            {
                console.Verbosity = command.Verbosity;
            }
        }
    }
}