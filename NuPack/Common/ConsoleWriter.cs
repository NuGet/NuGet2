namespace NuGet.Common {

    using System;
    using System.ComponentModel.Composition;

    [Export(typeof(IConsoleWriter))]
    public class ConsoleWriter : IConsoleWriter {

        public int CursorLeft {
            get {
                return Console.CursorLeft;
            }
            set {
                Console.CursorLeft = value;
            }
        }

        public int CursorTop {
            get {
                return Console.CursorTop;
            }
            set {
                Console.CursorTop = value;
            }
        }

        public string Title {
            get {
                return Console.Title;
            }
            set {
                Console.Title = value;
            }
        }

        public int WindowHeight {
            get {
                return Console.WindowHeight;
            }
            set {
                Console.WindowHeight = value;
            }
        }

        public int WindowWidth {
            get {
                return Console.WindowWidth;
            }
            set {
                Console.WindowWidth = value;
            }
        }

        public void Write(object value) {
            Console.Write(value);
        }

        public void Write(string value) {
            Console.Write(value);
        }

        public void Write(string format, params object[] arg) {
            Console.Write(format, arg);
        }

        public void WriteLine() {
            Console.WriteLine();
        }

        public void WriteLine(object value) {
            Console.WriteLine(value);
        }

        public void WriteLine(string value) {
            Console.WriteLine(value);
        }

        public void WriteLine(string format, params object[] arg) {
            Console.WriteLine(format, arg);
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
                // Calculate the number of chars to print based on the width of the console
                int length = Math.Min(text.Length, maxWidth);
                // Text we can print without overflowing the console.
                string content = text.Substring(0, length);
                int leftPadding = startIndex + length - CursorLeft;
                // Print it with the correct padding
                Console.WriteLine("{0," + leftPadding + "}", content);
                // Get the next substring to be printed
                text = text.Substring(content.Length);
            }
        }
    }
}
