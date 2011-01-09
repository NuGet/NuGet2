using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGetConsole.Host.PowerShell {
    /// <summary>
    /// Represents a parsed powershell command e.g. "Install-Package el -Version "
    /// </summary>
    public class Command {
        // Command arguments by name and index (That's why it's <object, string>)
        // "-Version " would be { "Version", "" } and
        // "-Version" would be { "Version", null }
        // Whitespace is significant wrt completion. We don't want to show intellisense for "-Version" but we do for "-Version "
        public Dictionary<object, string> Arguments { get; private set; }

        // Index of the argument we're trying to get completion for
        public int? CompletionIndex { get; set; }

        // Argument we're trying to get completion for
        public string CompletionArgument { get; set; }

        // Command name
        public string CommandName { get; set; }

        public Command() {
            Arguments = new Dictionary<object, string>();
        }
    }
}
