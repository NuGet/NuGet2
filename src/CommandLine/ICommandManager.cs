using System.Collections.Generic;
using System.Reflection;

namespace NuGet {
    public interface ICommandManager {
        IEnumerable<ICommand> GetCommands();
        ICommand GetCommand(string commandName);
        IDictionary<OptionAttribute, PropertyInfo> GetCommandOptions(ICommand command);
        void RegisterCommand(ICommand command);
    }
}
