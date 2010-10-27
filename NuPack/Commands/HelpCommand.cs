namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "help", "HelpCommandDescription", AltName = "?", MaxArgs = 1,
        UsageSummaryResourceName = "HelpCommandUsageDecription", UsageDescriptionResourceName = "HelpCommandUsageDecription")]
    public class HelpCommand : ICommand {
        private readonly string _commandExe;
        private readonly ICommandManager _commandManager;
        private readonly string _helpUrl;
        private readonly string _productName;

        public List<string> Arguments {
            get;
            set;
        }
        private string CommandName {
            get {
                if (Arguments != null && Arguments.Count > 0) {
                    return Arguments[0];
                }
                return null;
            }
        }

        [ImportingConstructor]
        public HelpCommand(ICommandManager commandManager)
            : this(commandManager, Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Name, null) {
        }

        public HelpCommand(ICommandManager commandManager, string commandExe, string productName, string helpUrl) {
            _commandManager = commandManager;
            _commandExe = commandExe;
            _productName = productName;
            _helpUrl = helpUrl;
        }

        public void Execute() {
            if (!String.IsNullOrEmpty(CommandName)) {
                ViewHelpForCommand(CommandName);
            }
            else {
                ViewHelp();
            }
        }

        public void ViewHelp() {
            Console.WriteLine("{0} Version: {1}", _productName, Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine("usage: {0} <command> [args] [options] ", _commandExe);
            Console.WriteLine("Type '{0} help <command>' for help on a specific command.", _commandExe);
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            Console.WriteLine();

            var commands = from c in _commandManager.GetCommands()
                           orderby c.Key.CommandName
                           select c.Key;

            // Padding for printing
            int maxWidth = commands.Max(c => c.CommandName.Length + GetAltText(c.AltName).Length);

            foreach (var command in commands) {
                PrintCommand(maxWidth, command);
            }

            if (_helpUrl != null) {
                Console.WriteLine();
                Console.WriteLine("For more information, visit {0}", _helpUrl);
            }
        }

        private void PrintCommand(int maxWidth, CommandAttribute commandAttribute) {
            // Write out the command name left justified with the max command's width's padding
            Console.Write(" {0, -" + maxWidth + "}   ", GetCommandText(commandAttribute));
            // Starting index of the description
            int descriptionPadding = maxWidth + 4;
            PrintJustified(descriptionPadding, commandAttribute.GetDescription());
        }

        private void PrintJustified(int startIndex, string text) {
            PrintJustified(startIndex, text, GetConsoleWidth());
        }

        private void PrintJustified(int startIndex, string text, int maxWidth) {
            if (maxWidth > startIndex) {
                maxWidth = maxWidth - startIndex - 1;
            }

            while (text.Length > 0) {
                // Trim whitespace at the beginning
                text = text.TrimStart();
                // Calculate the number of chars to print based on the width of the console
                int length = Math.Min(text.Length, maxWidth);
                // Text we can print without overflowing the console.
                string content = text.Substring(0, length);
                int leftPadding = startIndex + length - GetConsoleCursorLeft();
                // Print it with the correct padding
                Console.WriteLine("{0," + leftPadding + "}", content);
                // Get the next substring to be printed
                text = text.Substring(content.Length);
            }
        }

        private string GetCommandText(CommandAttribute commandAttribute) {
            return commandAttribute.CommandName + GetAltText(commandAttribute.AltName);
        }

        public void ViewHelpForCommand(string commandName) {
            ICommand command = _commandManager.GetCommand(commandName);

            if (command == null) {
                throw new CommandLineException(NuGetResources.UnknowCommandError, commandName);
            }

            CommandAttribute attribute = _commandManager.GetCommandAttribute(command);

            Console.WriteLine("usage: {0} {1}", _commandExe, attribute.CommandName);
            Console.WriteLine();

            if (!String.IsNullOrEmpty(attribute.AltName)) {
                Console.WriteLine("alias: {0}", attribute.AltName);
                Console.WriteLine();
            }

            Console.WriteLine(attribute.GetDescription());
            Console.WriteLine();

            if (attribute.GetUsageDescription() != null) {
                int padding = 5;
                PrintJustified(padding, attribute.GetUsageDescription(), GetConsoleWidth() - padding);
                Console.WriteLine();
            }

            IDictionary<OptionAttribute, PropertyInfo> options = _commandManager.GetCommandOptions(command);

            if (options.Count > 0) {
                Console.WriteLine("options:");
                Console.WriteLine();

                //Get the max option width
                int maxOptionWidth = options.Max(o => o.Value.Name.Length);
                //Get the max altname option width
                int maxAltOptionWidth = options.Max(o => o.Key.AltName.Length);


                foreach (var o in options) {
                    Console.Write(" {0, -" + (maxOptionWidth + 2) + "}", o.Value.Name);
                    Console.Write(" {0, -" + (maxAltOptionWidth + 4) + "}", "(" + o.Key.AltName + ")");
                    PrintJustified((10 + maxAltOptionWidth + maxOptionWidth), o.Key.GetDescription());

                }
                Console.WriteLine();
            }
        }

        /* Until we can abstract the Console Object we need to catch IOException 
         * (the exception that happens if the property has not been set) which 
         * is thrown from WindowWidth and CursorLeft when there is no window aka 
         * in the integration tests. We can later change this so that we can 
         * pass in our own console object and keep these errors from being thrown. */

        private static int GetConsoleWidth() {
            int maxWidth;
            try {
                maxWidth = Console.WindowWidth;
            }
            catch (System.IO.IOException) {
                maxWidth = 60;
            }
            return maxWidth;
        }

        private static int GetConsoleCursorLeft() {
            int cursorLeft;
            try {
                cursorLeft = Console.CursorLeft;
            }
            catch (System.IO.IOException) {
                cursorLeft = 0;
            }
            return cursorLeft;
        }

        private static string GetAltText(string altNameText) {
            if (String.IsNullOrEmpty(altNameText)) {
                return String.Empty;
            }
            return String.Format(CultureInfo.CurrentCulture, " ({0})", altNameText);
        }

    }
}
