using System.IO;

namespace NuGet.Common {
    public interface IConsole : ILogger {
        int CursorLeft { get; set; }
        TextWriter Error { get; }
        int WindowWidth { get; set; }

        void Write(object value);
        void Write(string value);
        void Write(string format, params object[] args);
        void WriteLine();
        void WriteLine(object value);
        void WriteLine(string value);
        void WriteLine(string format, params object[] args);
        void WriteError(object value);
        void WriteError(string value);
        void WriteError(string format, params object[] args);
        void WriteWarning(string value);
        void WriteWarning(string value, params object[] args);

        bool Confirm(string description);

        void PrintJustified(int startIndex, string text);
        void PrintJustified(int startIndex, string text, int maxWidth);
    }
}
