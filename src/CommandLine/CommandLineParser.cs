namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NuGet.Common;
    using System.Linq;

    public class CommandLineParser {
        private ICommandManager _commandManager;

        public CommandLineParser(ICommandManager manager) {
            _commandManager = manager;
            _index = 0;
        }

        public ICommand ExtractOptions(ICommand command, List<string> commandlineArgs) {
            List<string> arguments = new List<string>();
            
            IDictionary<OptionAttribute, PropertyInfo> properties = _commandManager.GetCommandOptions(command);

            while (true) {
                string option = GetNextCommandLineItem(commandlineArgs);

                if (option == null) {
                    break;
                }

                if (!option.StartsWith("/") && !option.StartsWith("-")) {
                    arguments.Add(option);
                    continue;
                }

                PropertyInfo propInfo = null;
                string optionText = option.Substring(1);
                string value = null;

                if (optionText.EndsWith("-")) {
                    optionText = optionText.TrimEnd('-');
                    value = "false";
                }

                foreach (KeyValuePair<OptionAttribute, PropertyInfo> pair in properties) {
                    if (String.Equals(pair.Value.Name, optionText, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(pair.Key.AltName, optionText, StringComparison.OrdinalIgnoreCase)) {
                        propInfo = pair.Value;
                        break;
                    }
                }

                if (propInfo == null) {
                    throw new CommandLineException(NuGetResources.UnknownOptionError, option);
                }

                if (propInfo.PropertyType == typeof(bool)) {
                    value = value ?? "true";
                }
                else {
                    value = GetNextCommandLineItem(commandlineArgs);
                }

                if (value == null) {
                    throw new CommandLineException(NuGetResources.MissingOptionValueError, option);
                }

                try {
                    propInfo.SetValue(command, CommandLineUtility.ChangeType(value, propInfo.PropertyType), null);
                }
                catch {
                    throw new CommandLineException(NuGetResources.InvalidOptionValueError, option, value);
                }
            }

            command.Arguments = arguments;            

            return command;
        }

        public ICommand ParseCommandLine(List<string> commandlineArgs) {
            // Get the desired command name
            string cmdName = GetNextCommandLineItem(commandlineArgs);
            if (cmdName == null) {
                return null;
            }

            // Get the command based on the name
            ICommand cmd = _commandManager.GetCommand(cmdName);
            if (cmd == null) {
                throw new CommandLineException(NuGetResources.UnknowCommandError, cmdName);
            }

            return ExtractOptions(cmd, commandlineArgs);
        }

        public static string GetNextCommandLineItem(List<string> commandlineArgs) {
            if (commandlineArgs == null || !commandlineArgs.Any()) {
                return null;
            }

            var item = commandlineArgs.First();
            commandlineArgs.RemoveAt(0);
            return item;
        }
    }
}
