using System;
using System.Text;
using NuGetConsole;

namespace NuGet.VisualStudio
{
    public class SmartOutputConsoleProvider : IOutputConsoleProvider
    {
        private readonly IOutputConsoleProvider _baseProvider;
        private BufferedOutputConsole _bufferedConsole;

        public SmartOutputConsoleProvider(IOutputConsoleProvider baseProvider)
        {
            _baseProvider = baseProvider;
        }

        public IConsole CreateOutputConsole(bool requirePowerShellHost)
        {
            IConsole console = _baseProvider.CreateOutputConsole(requirePowerShellHost);

            if (_bufferedConsole == null)
            {
                _bufferedConsole = new BufferedOutputConsole(console);
            }

            return _bufferedConsole;
        }

        public void Flush()
        {
            if (_bufferedConsole != null)
            {
                _bufferedConsole.Flush();
            }
        }

        /// <summary>
        /// Clears the output in the console
        /// </summary>
        public void Clear()
        {
            if (_bufferedConsole != null)
            {
                _bufferedConsole.Clear();
            }
        }

        private class BufferedOutputConsole : IConsole
        {
            private readonly IConsole _baseConsole;
            private readonly StringBuilder _messages = new StringBuilder();

            public BufferedOutputConsole(IConsole baseConsole)
            {
                _baseConsole = baseConsole;
            }

            public IHost Host
            {
                get
                {
                    return _baseConsole.Host;
                }
                set
                {
                    _baseConsole.Host = value;
                }
            }

            public bool ShowDisclaimerHeader
            {
                get { return _baseConsole.ShowDisclaimerHeader; }
            }

            public IConsoleDispatcher Dispatcher
            {
                get { return _baseConsole.Dispatcher; }
            }

            public int ConsoleWidth
            {
                get { return _baseConsole.ConsoleWidth; }
            }

            public void WriteProgress(string currentOperation, int percentComplete)
            {
                // ignore this because the output window doesn't show progress anyway
            }

            public void Write(string text)
            {
                _messages.Append(text);
            }

            public void WriteLine(string text)
            {
                _messages.AppendLine(text);
            }

            public void Write(string text, System.Windows.Media.Color? foreground, System.Windows.Media.Color? background)
            {
                Write(text);
            }

            public void WriteBackspace()
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                _baseConsole.Clear();
                _messages.Clear();
            }

            public void Flush()
            {
                if (_messages.Length > 0)
                {
                    _baseConsole.WriteLine(_messages.ToString());
                    _messages.Clear();
                }
            }
        }
    }
}