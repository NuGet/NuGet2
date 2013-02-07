using System;
using System.IO;
using System.Text;
using NuGet.Common;

namespace NuGet.Test
{
    public sealed class MockConsole : IConsole
    {
        private readonly StringBuilder builder = new StringBuilder();

        public int CursorLeft
        {
            get;
            set;
        }

        public String Output
        {
            get { return builder.ToString(); }
        }

        public Verbosity Verbosity
        {
            get;
            set;
        }

        public bool IsNonInteractive
        {
            get;
            set;
        }

        public int WindowWidth
        {
            get;
            set;
        }

        public void Write(object value)
        {
            builder.Append(value);
        }

        public void Write(string value)
        {
            builder.Append(value);
        }

        public void Write(string format, params object[] args)
        {
            builder.AppendFormat(format, args);
        }

        public void WriteLine()
        {
            builder.AppendLine();
        }

        public void WriteLine(object value)
        {
            builder.AppendLine(value.ToString());
        }

        public void WriteLine(string value)
        {
            builder.AppendLine(value);
        }

        public void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            WriteLine();
        }

        public void WriteLine(ConsoleColor color, string value, params object[] args)
        {
            WriteLine(value, args);
        }

        public void WriteError(object value)
        {
            Write(value);
        }

        public void WriteError(string value)
        {
            Write(value);
        }

        public void WriteError(string format, params object[] args)
        {
            Write(format, args);
        }

        public void WriteWarning(string value)
        {
            Write(value);
        }

        public void WriteWarning(bool prependWarningText, string value)
        {
            Write(value);
        }

        public void WriteWarning(string value, params object[] args)
        {
            Write(value, args);
        }

        public void WriteWarning(bool prependWarningText, string value, params object[] args)
        {
            Write(value, args);
        }

        public bool Confirm(string description)
        {
            throw new NotImplementedException();
        }

        public void PrintJustified(int startIndex, string text)
        {
            Write(text);
        }

        public void PrintJustified(int startIndex, string text, int maxWidth)
        {
            Write(text);
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            Write(message, args);
        }

        public ConsoleKeyInfo ReadKey()
        {
            throw new NotSupportedException();
        }

        public string ReadLine()
        {
            throw new NotSupportedException();
        }

        public void ReadSecureString(System.Security.SecureString secureString)
        {
            throw new NotSupportedException();
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.Ignore;
        }

        public void ResetFileConflictResolution()
        {
        }
    }
}
