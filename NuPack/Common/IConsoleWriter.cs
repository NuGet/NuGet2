namespace NuGet.Common {

    public interface IConsoleWriter {
        int CursorLeft { get; set; }
        int CursorTop { get; set; }
        string Title { get; set; }
        int WindowHeight { get; set; }
        int WindowWidth { get; set; }

        void Write(object value);
        void Write(string value);
        void Write(string format, params object[] arg);
        void WriteLine();
        void WriteLine(object value);
        void WriteLine(string value);
        void WriteLine(string format, params object[] arg);

        void PrintJustified(int startIndex, string text);
        void PrintJustified(int startIndex, string text, int maxWidth);
    }
}
