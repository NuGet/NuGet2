namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NuGet.Common;

    public class CommandLineParser {
        private ICommandManager _commandManager;

        public CommandLineParser(ICommandManager manager) {
            _commandManager = manager;
        }

        public bool ArgCountTooHigh(ICommand command) {
            return command.Arguments.Count > _commandManager.GetCommandAttribute(command).MaxArgs;
        }

        public bool ArgCountTooLow(ICommand command) {
            return command.Arguments.Count < _commandManager.GetCommandAttribute(command).MinArgs;
        }

        public ICommand ExtractOptions(ICommand command, string commandLine) {
            List<string> arguments = new List<string>();
            IDictionary<OptionAttribute, PropertyInfo> properties = _commandManager.GetCommandOptions(command);

            while (true) {
                string option = GetNextCommandLineItem(ref commandLine);

                if (option == String.Empty) {
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
                    value = GetNextCommandLineItem(ref commandLine);
                }

                if (value == String.Empty) {
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

            if (ArgCountTooHigh(command)) {
                throw new CommandLineException(NuGetResources.TooManyArgsError);
            }

            else if (ArgCountTooLow(command)) {
                throw new CommandLineException(NuGetResources.TooFewArgsError);
            }

            return command;
        }

        public ICommand ParseCommandLine(string commandLine) {
            // Extract the executable name
            GetNextCommandLineItem(ref commandLine);

            // Get the desired command name
            string cmdName = GetNextCommandLineItem(ref commandLine);
            if (cmdName == String.Empty) {
                return null;
            }

            // Get the command based on the name
            ICommand cmd = _commandManager.GetCommand(cmdName);
            if (cmd == null) {
                throw new CommandLineException(NuGetResources.UnknowCommandError, cmdName);
            }

            return ExtractOptions(cmd, commandLine);
        }


        public static string GetNextCommandLineItem(ref string commandLine) {
            bool inQuotes = false;
            int idx = 0;

            commandLine = commandLine.Trim();

            while (idx < commandLine.Length) {
                if (commandLine[idx] == ' ' && !inQuotes) {
                    break;
                }

                if (commandLine[idx] == '"') {
                    inQuotes = !inQuotes;
                }

                ++idx;
            }

            string result = commandLine.Substring(0, idx);

            if (idx < commandLine.Length) {
                commandLine = commandLine.Substring(idx + 1);
            }
            else {
                commandLine = String.Empty;
            }

            return result.Trim('"');
        }
    }
}
