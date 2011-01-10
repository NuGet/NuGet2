using System;
using System.Text;

namespace NuGetConsole.Host.PowerShell {
    /// <summary>
    /// A simple parser used for parsing commands that require completion (intellisense).
    /// </summary>
    public class CommandParser {
        private int _index;
        private readonly string _command;

        private CommandParser(string command) {
            _command = command;
        }

        private char CurrentChar {
            get {
                return _index < _command.Length ? _command[_index] : '\0';
            }
        }
        
        private bool Done {
            get {
                return _index >= _command.Length;
            }
        }

        public static Command Parse(string command) {
            return new CommandParser(command).ParseCore();
        }

        private Command ParseCore() {
            int positionalArgumentIndex = 0;
            var parsedCommand = new Command();

            // Get the command name
            parsedCommand.CommandName = ParseToken();

            while (!Done) {
                string argument = null;

                if (SkipWhitespace()) {
                    argument = ParseToken();
                }

                if (argument.StartsWith("-", StringComparison.Ordinal)) {
                    // Trim the -
                    argument = argument.Substring(1);

                    if (!String.IsNullOrEmpty(argument)) {
                        // Parse the argument value if any
                        if (SkipWhitespace() && CurrentChar != '-') {
                            parsedCommand.Arguments[argument] = ParseToken();
                        }
                        else {
                            parsedCommand.Arguments[argument] = null;
                        }

                        parsedCommand.CompletionArgument = argument;
                    }
                    else {
                        // If this was an empty argument then we aren't trying to complete anything
                        parsedCommand.CompletionArgument = null;
                    }

                    // Reset the completion index if we're completing an argument (these 2 properties are mutually exclusive)
                    parsedCommand.CompletionIndex = null;
                }
                else {
                    // Reset the completion argument
                    parsedCommand.CompletionArgument = null;
                    parsedCommand.CompletionIndex = positionalArgumentIndex;
                    parsedCommand.Arguments[positionalArgumentIndex++] = argument;
                }
            }

            return parsedCommand;
        }

        private string ParseQuotes() {
            // Skip the first '
            ParseChar();
            var sb = new StringBuilder();
            while (!Done) {
                sb.Append(ParseUntil(c => c == '\''));
                ParseChar();

                if (CurrentChar == '\'') {
                    sb.Append(ParseChar());
                }
                else {
                    break;
                }
            }

            return sb.ToString();
        }

        private char ParseChar() {
            char ch = CurrentChar;
            _index++;
            return ch;
        }

        private string ParseToken() {
            if (CurrentChar == '\'') {
                return ParseQuotes();
            }
            return ParseUntil(Char.IsWhiteSpace);
        }

        private string ParseUntil(Func<char, bool> predicate) {
            var sb = new StringBuilder();
            while (!Done && !predicate(CurrentChar)) {
                sb.Append(CurrentChar);
                _index++;
            }
            return sb.ToString();
        }

        private bool SkipWhitespace() {
            string ws = ParseUntil(c => !Char.IsWhiteSpace(c));
            return ws.Length > 0;
        }
    }
}