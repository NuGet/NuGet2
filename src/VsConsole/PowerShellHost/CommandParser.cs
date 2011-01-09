using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetConsole.Host.PowerShell {
    public class Command {
        public Dictionary<object, string> Arguments { get; set; }
        public int? CompletionIndex { get; set; }
        public string CompletionArgument { get; set; }
        public string CommandName { get; set; }

        public Command() {
            Arguments = new Dictionary<object, string>();
        }
    }

    public class CommandParser {
        private int _index;
        private readonly string _command;

        private CommandParser(string command) {
            _command = command;
        }

        private char CurrentChar {
            get {
                if (_index < _command.Length) {
                    return _command[_index];
                }
                return '\0';
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
            parsedCommand.CommandName = ParseUntilWhitespace();

            while (!Done) {
                string argument = null;

                if (SkipWhitespace()) {
                    argument = ParseUntilWhitespace();
                }

                if (argument.StartsWith("-")) {
                    argument = argument.Substring(1);

                    if (!String.IsNullOrEmpty(argument)) {
                        if (SkipWhitespace() && CurrentChar != '-') {
                            parsedCommand.Arguments[argument] = ParseUntilWhitespace();
                        }
                        else {
                            parsedCommand.Arguments[argument] = null;
                        }
                        parsedCommand.CompletionArgument = argument;
                    }
                    else {
                        parsedCommand.CompletionArgument = null;
                    }

                    parsedCommand.CompletionIndex = null;
                }
                else {
                    parsedCommand.CompletionArgument = null;
                    parsedCommand.CompletionIndex = positionalArgumentIndex;
                    parsedCommand.Arguments[positionalArgumentIndex++] = argument;
                }
            }

            return parsedCommand;
        }

        private string ParseUntilWhitespace() {
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