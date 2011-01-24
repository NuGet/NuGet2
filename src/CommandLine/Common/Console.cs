using System;
using System.ComponentModel.Composition;
using System.IO;

namespace NuGet.Common {
    [Export(typeof(IConsole))]
    public class Console : IConsole {

        public int CursorLeft {
            get {
                try {
                    return System.Console.CursorLeft;
                }
                catch (IOException) {
                    return 0;
                }
            }
            set {
                System.Console.CursorLeft = value;
            }
        }

        public int WindowWidth {
            get {
                try {
                    return System.Console.WindowWidth;
                }
                catch (IOException) {
                    return 60;
                }
            }
            set {
                System.Console.WindowWidth = value;
            }
        }

        public TextWriter Error {
            get { return System.Console.Error; }
        }

        public void Write(object value) {
            System.Console.Write(value);
        }

        public void Write(string value) {
            System.Console.Write(value);
        }

        public void Write(string format, params object[] arg) {
            System.Console.Write(format, arg);
        }

        public void WriteLine() {
            System.Console.WriteLine();
        }

        public void WriteLine(object value) {
            System.Console.WriteLine(value);
        }

        public void WriteLine(string value) {
            System.Console.WriteLine(value);
        }

        public void WriteLine(string format, params object[] arg) {
            System.Console.WriteLine(format, arg);
        }

        public void WriteError(object value) {
            WriteError(value.ToString());
        }

        public void WriteError(string value) {
            WriteError(value, null);
        }

        public void WriteError(string format, params object[] arg) {
            var currentColor = System.ConsoleColor.Gray;
            try {
                currentColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.Error.WriteLine(format, arg);
            }
            finally {
                System.Console.ForegroundColor = currentColor;
            }
        }

        public void PrintJustified(int startIndex, string text) {
            PrintJustified(startIndex, text, WindowWidth);
        }

        public void PrintJustified(int startIndex, string text, int maxWidth) {
            if (maxWidth > startIndex) {
                maxWidth = maxWidth - startIndex - 1;
            }

            while (text.Length > 0) {
                // Trim whitespace at the beginning
                text = text.TrimStart();
                // Calculate the number of chars to print based on the width of the System.Console
                int length = Math.Min(text.Length, maxWidth);
                // Text we can print without overflowing the System.Console.
                string content = text.Substring(0, length);
                int leftPadding = startIndex + length - CursorLeft;
                // Print it with the correct padding
                System.Console.WriteLine("{0," + leftPadding + "}", content);
                // Get the next substring to be printed
                text = text.Substring(content.Length);
            }
        }

        public bool Confirm(string message) {
            var currentColor = System.ConsoleColor.Gray;
            try {
                currentColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                System.Console.Write(String.Format(NuGetResources.ConsoleConfirmMessage, message));
                var result = System.Console.ReadLine();
                return result.StartsWith(NuGetResources.ConsoleConfirmMessageAccept, StringComparison.InvariantCultureIgnoreCase);
            }
            finally {
                System.Console.ForegroundColor = currentColor;
            }
        }
    }
}
