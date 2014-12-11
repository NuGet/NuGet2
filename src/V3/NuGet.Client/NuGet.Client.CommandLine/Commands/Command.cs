using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet.Common;
using System.Diagnostics;

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

        [Option(typeof(NuGetCommandResourceType), "Option_Verbosity")]
        public Verbosity Verbosity { get; set; }

        [Option(typeof(NuGetCommandResourceType), "Option_NonInteractive")]
        public bool NonInteractive { get; set; }

        [Option(typeof(NuGetCommandResourceType), "Option_ConfigFile")]
        public string ConfigFile { get; set; }

        protected internal ISettings Settings { get; set; }

        protected internal IPackageSourceProvider SourceProvider { get; set; }

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
                Stopwatch watch = new Stopwatch();
                watch.Start();
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

                    // Create the config file when neccessary
                    if (!configFileSystem.FileExists(configFileName) &&
                        ShouldCreateConfigFile)
                    {
                        XmlUtility.CreateDocument("configuration", configFileSystem, configFileName);
                    }

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

                ExecuteCommand();
                watch.Stop();
                DisplayExecutedTime(watch.Elapsed, CommandAttribute.CommandName);
            }
        }

        public abstract void ExecuteCommand();

        protected void DisplayExecutedTime(TimeSpan elapsed, string executionName)
        {
            if (Verbosity == NuGet.Verbosity.Detailed)
            {
                Console.WriteLine("Executed '{0}' in {1} seconds", executionName, elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Indicates if the config file should be created if it does not exist.
        /// </summary>
        protected virtual bool ShouldCreateConfigFile
        {
            get
            {
                return false;
            }
        }

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
