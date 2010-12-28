namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Reflection;
    using NuGet.Common;

    [Export(typeof(ICommandManager))]
    public class CommandManager : ICommandManager {
        private readonly Dictionary<CommandAttribute, ICommand> _commands;

        public CommandManager() {
            _commands = new Dictionary<CommandAttribute, ICommand>();
        }

        public IDictionary<CommandAttribute, ICommand> GetCommands() {
            return _commands;
        }

        public CommandAttribute GetCommandAttribute(ICommand command) {
            foreach (KeyValuePair<CommandAttribute, ICommand> pair in _commands)
                if (pair.Value.GetType() == command.GetType()) {
                    return pair.Key;
                }
            return null;
        }

        public ICommand GetCommand(string commandName) {
            foreach (KeyValuePair<CommandAttribute, ICommand> kvp in _commands) {
                if (String.Equals(kvp.Key.CommandName, commandName, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(kvp.Key.AltName, commandName, StringComparison.OrdinalIgnoreCase)) {
                    return kvp.Value;
                }
            }
            return null;
        }

        public IDictionary<OptionAttribute, PropertyInfo> GetCommandOptions(ICommand command) {
            Dictionary<OptionAttribute, PropertyInfo> result = new Dictionary<OptionAttribute, PropertyInfo>();

            foreach (PropertyInfo propInfo in command.GetType().GetProperties()) {
                foreach (OptionAttribute attr in propInfo.GetCustomAttributes(typeof(OptionAttribute), true)) {
                    if (!propInfo.CanWrite) {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                            NuGetResources.OptionInvalidWithoutSetter, command.GetType().FullName + "." + propInfo.Name));
                    }
                    result.Add(attr, propInfo);
                }
            }

            return result;
        }

        public void RegisterCommand(ICommand command) {
            foreach (CommandAttribute attrib in command.GetType().GetCustomAttributes(typeof(CommandAttribute), true)) {
                _commands.Add(attrib, command);
            }
        }
    }
}
