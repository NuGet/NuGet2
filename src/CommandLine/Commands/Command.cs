using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet.Commands
{
    public abstract class Command : ICommand
    {
        private const string CommandSuffix = "Command";
        private CommandAttribute _commandAttribute;

        protected Command()
        {
            Arguments = new List<string>();
        }

        [Import]
        public IFileSystem FileSystem { get; set; }

        public IList<string> Arguments { get; private set; }

        [Import]
        public IConsole Console { get; set; }

        [Import]
        public HelpCommand HelpCommand { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        [Import]
        public IMachineWideSettings MachineWideSettings { get; set; }

        [Option("help", AltName = "?")]
        public bool Help { get; set; }

        [Option(typeof(NuGetCommand), "Option_Verbosity")]
        public Verbosity Verbosity { get; set; }

        [Option(typeof(NuGetCommand), "Option_NonInteractive")]
        public bool NonInteractive { get; set; }

        [Option(typeof(NuGetCommand), "Option_ConfigFile")]
        public string ConfigFile { get; set; }

        protected internal ISettings Settings { get; set; }

        protected internal IPackageSourceProvider SourceProvider { get; set; }

        protected internal IPackageRepositoryFactory RepositoryFactory { get; set; }

        public CommandAttribute CommandAttribute
        {
            get
            {
                if (_commandAttribute == null)
                {
                    _commandAttribute = GetCommandAttribute();
                }
                return _commandAttribute;
            }
        }

        public virtual bool IncludedInHelp(string optionName)
        {
            return true;
        }

        public void Execute()
        {
            if (Help)
            {
                HelpCommand.ViewHelpForCommand(CommandAttribute.CommandName);
            }
            else
            {
                if (String.IsNullOrEmpty(ConfigFile))
                {
                    Settings = NuGet.Settings.LoadDefaultSettings(
                        FileSystem, 
                        configFileName: null, 
                        machineWideSettings: MachineWideSettings);
                }
                else
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(ConfigFile));
                    var configFileName = Path.GetFileName(ConfigFile);
                    var configFileSystem = new PhysicalFileSystem(directory);
                    configFileSystem.Logger = Console;
                    Settings = NuGet.Settings.LoadDefaultSettings(
                        configFileSystem,
                        configFileName,
                        MachineWideSettings);
                }

                SourceProvider = PackageSourceBuilder.CreateSourceProvider(Settings);

                // Register an additional provider for the console specific application so that the user
                // will be prompted if a proxy is set and credentials are required
                var credentialProvider = new SettingsCredentialProvider(
                    new ConsoleCredentialProvider(Console),
                    SourceProvider, 
                    Console);
                HttpClient.DefaultCredentialProvider = credentialProvider;
                RepositoryFactory = new NuGet.Common.CommandLineRepositoryFactory(Console);

                ExecuteCommand();
            }
        }

        public abstract void ExecuteCommand();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method does quite a bit of processing.")]
        public virtual CommandAttribute GetCommandAttribute()
        {
            var attributes = GetType().GetCustomAttributes(typeof(CommandAttribute), true);
            if (attributes.Any())
            {
                return (CommandAttribute)attributes.FirstOrDefault();
            }

            // Use the command name minus the suffix if present and default description
            string name = GetType().Name;
            int idx = name.LastIndexOf(CommandSuffix, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                name = name.Substring(0, idx);
            }
            if (!String.IsNullOrEmpty(name))
            {
                return new CommandAttribute(name, LocalizedResourceManager.GetString("DefaultCommandDescription"));
            }
            return null;
        }
    }
}
