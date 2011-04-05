using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;

namespace NuGetConsole.Implementation.Console {
    internal interface IPrivateConsoleDispatcher : IConsoleDispatcher {
        event EventHandler<EventArgs<Tuple<SnapshotSpan, bool>>> ExecuteInputLine;
        void PostInputLine(InputLine inputLine);
    }

    /// <summary>
    /// This class handles input line posting and command line dispatching/execution.
    /// </summary>
    internal class ConsoleDispatcher : IPrivateConsoleDispatcher {
        /// <summary>
        /// The IPrivateWpfConsole instance this dispatcher works with.
        /// </summary>
        private IPrivateWpfConsole WpfConsole { get; set; }

        /// <summary>
        /// Child dispatcher based on host type. Its creation is postponed to Start(), so that
        /// a WpfConsole's dispatcher can be accessed while inside a host construction.
        /// </summary>
        private Dispatcher _dispatcher;

        public ConsoleDispatcher(IPrivateWpfConsole wpfConsole) {
            UtilityMethods.ThrowIfArgumentNull(wpfConsole);
            this.WpfConsole = wpfConsole;
        }

        public bool IsExecutingCommand {
            get {
                return (_dispatcher == null) ? false : _dispatcher.IsExecuting;
            }
        }

        public event EventHandler StartCompleted;

        public bool IsStartCompleted { get; private set; }

        #region IConsoleDispatcher

        public void Start() {
            // Only Start once
            if (_dispatcher == null) {
                IHost host = WpfConsole.Host;

                if (host == null) {
                    throw new InvalidOperationException("Can't start Console dispatcher. Host is null.");
                }

                if (host is IAsyncHost) {
                    _dispatcher = new AsyncHostConsoleDispatcher(this);
                }
                else {
                    _dispatcher = new SyncHostConsoleDispatcher(this);
                }
                
                Task.Factory.StartNew(
                    // gives the host a chance to do initialization works before the console starts accepting user inputs
                    () => host.Initialize(WpfConsole)  
                ).ContinueWith(
                    task => {
                        if (task.IsFaulted) {
                            var exception = task.Exception;
                            WriteError((exception.InnerException ?? exception).Message);
                        }

                        if (host.IsCommandEnabled) {
                            Microsoft.VisualStudio.Shell.ThreadHelper.Generic.Invoke(_dispatcher.Start);
                        }

                        if (StartCompleted != null) {
                            Microsoft.VisualStudio.Shell.ThreadHelper.Generic.Invoke(() => StartCompleted(this, EventArgs.Empty));
                        }
                        IsStartCompleted = true;
                    },
                    TaskContinuationOptions.NotOnCanceled
                );
            }
        }

        private void WriteError(string message) {
            if (WpfConsole != null) {
                WpfConsole.Write(message + Environment.NewLine, Colors.Red, null);
            }
        }

        public void ClearConsole() {
            Debug.Assert(_dispatcher != null);
            if (_dispatcher != null) {
                _dispatcher.ClearConsole();
            }
        }
        #endregion

        #region IPrivateConsoleDispatcher
        public event EventHandler<EventArgs<Tuple<SnapshotSpan, bool>>> ExecuteInputLine;

        void OnExecute(SnapshotSpan inputLineSpan, bool isComplete) {
            ExecuteInputLine.Raise(this, Tuple.Create(inputLineSpan, isComplete));
        }

        public void PostInputLine(InputLine inputLine) {
            Debug.Assert(_dispatcher != null);
            if (_dispatcher != null) {
                _dispatcher.PostInputLine(inputLine);
            }
        }
        #endregion

        private abstract class Dispatcher {
            protected ConsoleDispatcher ParentDispatcher { get; private set; }
            protected IPrivateWpfConsole WpfConsole { get; private set; }

            private bool _isExecuting;

            public bool IsExecuting {
                get {
                    return _isExecuting;
                }
                protected set {
                    _isExecuting = value;
                    WpfConsole.SetExecutionMode(_isExecuting);
                }
            }

            protected Dispatcher(ConsoleDispatcher parentDispatcher) {
                ParentDispatcher = parentDispatcher;
                WpfConsole = parentDispatcher.WpfConsole;
            }

            /// <summary>
            /// Process a input line.
            /// </summary>
            /// <param name="inputLine"></param>
            protected Tuple<bool, bool> Process(InputLine inputLine) {
                SnapshotSpan inputSpan = inputLine.SnapshotSpan;

                if (inputLine.Flags.HasFlag(InputLineFlag.Echo)) {
                    WpfConsole.BeginInputLine();

                    if (inputLine.Flags.HasFlag(InputLineFlag.Execute)) {
                        WpfConsole.WriteLine(inputLine.Text);
                        inputSpan = WpfConsole.EndInputLine(true).Value;
                    }
                    else {
                        WpfConsole.Write(inputLine.Text);
                    }
                }

                if (inputLine.Flags.HasFlag(InputLineFlag.Execute)) {
                    string command = inputLine.Text;
                    bool isExecuted = WpfConsole.Host.Execute(WpfConsole, command, null);
                    WpfConsole.InputHistory.Add(command);
                    ParentDispatcher.OnExecute(inputSpan, isExecuted);
                    return Tuple.Create(true, isExecuted);
                }
                return Tuple.Create(false, false);
            }

            public void PromptNewLine() {
                WpfConsole.Write(WpfConsole.Host.Prompt + (char)32);    // 32 is the space
                WpfConsole.BeginInputLine();
            }

            public void ClearConsole() {
                // When inputting commands
                if (WpfConsole.InputLineStart != null) {
                    WpfConsole.Host.Abort(); // Clear constructing multi-line command
                    WpfConsole.Clear();
                    PromptNewLine();
                }
                else {
                    WpfConsole.Clear();
                }
            }

            public abstract void Start();
            public abstract void PostInputLine(InputLine inputLine);
        }

        /// <summary>
        /// This class dispatches inputs for synchronous hosts.
        /// </summary>
        private class SyncHostConsoleDispatcher : Dispatcher {
            public SyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher) {
            }

            public override void Start() {
                PromptNewLine();
            }

            public override void PostInputLine(InputLine inputLine) {
                IsExecuting = true;
                try {
                    if (Process(inputLine).Item1) {
                        PromptNewLine();
                    }
                }
                finally {
                    IsExecuting = false;
                }
            }
        }

        /// <summary>
        /// This class dispatches inputs for asynchronous hosts.
        /// </summary>
        private class AsyncHostConsoleDispatcher : Dispatcher {
            private Queue<InputLine> _buffer;
            private Marshaler _marshaler;

            public AsyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher) {
                _marshaler = new Marshaler(this);
            }

            private bool IsStarted {
                get {
                    return _buffer != null;
                }
            }

            public override void Start() {
                if (IsStarted) {
                    // Can only start once... ConsoleDispatcher is already protecting this.
                    throw new InvalidOperationException();
                }
                _buffer = new Queue<InputLine>();

                IAsyncHost asyncHost = WpfConsole.Host as IAsyncHost;
                if (asyncHost == null) {
                    // ConsoleDispatcher is already checking this.
                    throw new InvalidOperationException();
                }

                asyncHost.ExecuteEnd += _marshaler.AsyncHost_ExecuteEnd;
                PromptNewLine();
            }

            public override void PostInputLine(InputLine inputLine) {
                // The editor should be completely readonly unless started.
                Debug.Assert(IsStarted);

                if (IsStarted) {
                    _buffer.Enqueue(inputLine);
                    ProcessInputs();
                }
            }

            private void ProcessInputs() {
                if (IsExecuting) {
                    return;
                }

                if (_buffer.Count > 0) {
                    InputLine inputLine = _buffer.Dequeue();
                    Tuple<bool, bool> executeState = Process(inputLine);
                    if (executeState.Item1) {
                        IsExecuting = true;

                        if (!executeState.Item2) {
                            // If NOT really executed, processing the same as ExecuteEnd event
                            OnExecuteEnd();
                        }
                    }
                }
            }

            private void OnExecuteEnd() {
                if (IsStarted) {
                    // Filter out noise. A host could execute private commands.
                    Debug.Assert(IsExecuting);
                    IsExecuting = false;

                    PromptNewLine();
                    ProcessInputs();
                }
            }

            /// <summary>
            /// This private Marshaler marshals async host event to main thread so that the dispatcher
            /// doesn't need to worry about threading.
            /// </summary>
            private class Marshaler : Marshaler<AsyncHostConsoleDispatcher> {
                public Marshaler(AsyncHostConsoleDispatcher impl)
                    : base(impl) {
                }

                public void AsyncHost_ExecuteEnd(object sender, EventArgs e) {
                    Invoke(() => _impl.OnExecuteEnd());
                }
            }
        }
    }

    [Flags]
    internal enum InputLineFlag {
        Echo = 1,
        Execute = 2
    }

    internal class InputLine {
        public SnapshotSpan SnapshotSpan { get; private set; }
        public string Text { get; private set; }
        public InputLineFlag Flags { get; private set; }

        public InputLine(string text, bool execute) {
            this.Text = text;
            this.Flags = InputLineFlag.Echo;

            if (execute) {
                this.Flags |= InputLineFlag.Execute;
            }
        }

        public InputLine(SnapshotSpan snapshotSpan) {
            this.SnapshotSpan = snapshotSpan;
            this.Text = snapshotSpan.GetText();
            this.Flags = InputLineFlag.Execute;
        }
    }
}