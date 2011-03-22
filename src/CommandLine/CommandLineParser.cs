using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NuGet.Common;

namespace NuGet {
    public class CommandLineParser {
        private readonly ICommandManager _commandManager;

        public CommandLineParser(ICommandManager manager) {
            _commandManager = manager;
        }

        public ICommand ExtractOptions(ICommand command, IEnumerator<string> argsEnumerator) {
            List<string> arguments = new List<string>();
            IDictionary<OptionAttribute, PropertyInfo> properties = _commandManager.GetCommandOptions(command);

            while (true) {
                string option = GetNextCommandLineItem(argsEnumerator);

                if (option == null) {
                    break;
                }

                if (!option.StartsWith("/") && !option.StartsWith("-")) {
                    arguments.Add(option);
                    continue;
                }


                string optionText = option.Substring(1);
                string value = null;

                if (optionText.EndsWith("-")) {
                    optionText = optionText.TrimEnd('-');
                    value = "false";
                }

                var results = from prop in properties
                              where prop.Value.Name.StartsWith(optionText, StringComparison.OrdinalIgnoreCase) ||
                              (prop.Key.AltName ?? String.Empty).StartsWith(optionText, StringComparison.OrdinalIgnoreCase)
                              select prop;

                if (!results.Any()) {
                    throw new CommandLineException(NuGetResources.UnknownOptionError, option);
                }

                PropertyInfo propInfo = results.First().Value;
                if (results.Skip(1).Any()) {
                    try {
                        // When multiple results are found, if there's an exact match, return it.
                        propInfo = results.First(c => c.Value.Name.Equals(optionText, StringComparison.OrdinalIgnoreCase)
                                || optionText.Equals(c.Key.AltName, StringComparison.OrdinalIgnoreCase)).Value;
                    }
                    catch (InvalidOperationException) {
                        throw new CommandLineException(String.Format(CultureInfo.CurrentCulture, NuGetResources.AmbiguousOption, optionText,
                            String.Join(" ", from c in results select c.Value.Name)));
                    }
                }

                if (propInfo.PropertyType == typeof(bool)) {
                    value = value ?? "true";
                }
                else {
                    value = GetNextCommandLineItem(argsEnumerator);
                }

                if (value == null) {
                    throw new CommandLineException(NuGetResources.MissingOptionValueError, option);
                }

                AssignValue(propInfo, command, option, value);
            }

            command.Arguments = arguments;

            return command;
        }

        private static void AssignValue(PropertyInfo property, ICommand command, string option, object value) {
            try {

                if (CommandLineUtility.IsMultiValuedProperty(property)) {
                    // If we were able to look up a parent of type ICollection<>, perform a Add operation on it.
                    // Note that we expect the value is a string.
                    var stringValue = value as string;
                    Debug.Assert(stringValue != null);

                    dynamic list = property.GetValue(command, null);
                    // The parameter value is one or more semi-colon separated values: nuget pack -option "foo;bar;baz"
                    foreach (var item in stringValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                        list.Add(item);
                    }
                }
                else {
                    property.SetValue(command, CommandLineUtility.ChangeType(value, property.PropertyType), null);
                }
            }
            catch {
                throw new CommandLineException(NuGetResources.InvalidOptionValueError, option, value);
            }
        }

        public ICommand ParseCommandLine(IEnumerable<string> commandlineArgs) {
            IEnumerator<string> argsEnumerator = commandlineArgs.GetEnumerator();

            // Get the desired command name
            string cmdName = GetNextCommandLineItem(argsEnumerator);
            if (cmdName == null) {
                return null;
            }

            // Get the command based on the name
            ICommand cmd = _commandManager.GetCommand(cmdName);
            if (cmd == null) {
                throw new CommandLineException(NuGetResources.UnknowCommandError, cmdName);
            }

            return ExtractOptions(cmd, argsEnumerator);
        }

        public static string GetNextCommandLineItem(IEnumerator<string> argsEnumerator) {
            if (argsEnumerator == null || !argsEnumerator.MoveNext()) {
                return null;
            }
            return argsEnumerator.Current;
        }
    }
}
