using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace NuGetConsole.Implementation.Console {
    interface IPrivateConsoleDispatcher : IConsoleDispatcher {
        event EventHandler<EventArgs<Tuple<SnapshotSpan, bool>>> ExecuteInputLine;
        void PostInputLine(InputLine inputLine);
    }

    /// <summary>
    /// This class handles input line posting and command line dispatching/execution.
    /// </summary>
    class ConsoleDispatcher : IPrivateConsoleDispatcher {
        /// <summary>
        /// The IPrivateWpfConsole instance this dispatcher works with.
        /// </summary>
        IPrivateWpfConsole WpfConsole { get; set; }

        /// <summary>
        /// Child dispatcher based on host type. Its creation is postponed to Start(), so that
        /// a WpfConsole's dispatcher can be accessed while inside a host construction.
        /// </summary>
        Dispatcher _dispatcher;

        public ConsoleDispatcher(IPrivateWpfConsole wpfConsole) {
            UtilityMethods.ThrowIfArgumentNull(wpfConsole);
            this.WpfConsole = wpfConsole;
        }

        #region IConsoleDispatcher
        public event EventHandler Starting;

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

                this.Starting.Raise(this);
                _dispatcher.Start();
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

        abstract class Dispatcher {
            protected ConsoleDispatcher ParentDispatcher { get; private set; }
            protected IPrivateWpfConsole WpfConsole { get; private set; }

            protected Dispatcher(ConsoleDispatcher parentDispatcher) {
                ParentDispatcher = parentDispatcher;
                WpfConsole = parentDispatcher.WpfConsole;
            }

            /// <summary>
            /// Process a input line.
            /// </summary>
            /// <param name="inputLine"></param>
            /// <returns></returns>
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
                    bool isExecuted = WpfConsole.Host.Execute(command);
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
        class SyncHostConsoleDispatcher : Dispatcher {
            public SyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher) {
            }

            public override void Start() {
                PromptNewLine();
            }

            public override void PostInputLine(InputLine inputLine) {
                if (Process(inputLine).Item1) {
                    PromptNewLine();
                }
            }
        }

        /// <summary>
        /// This class dispatches inputs for asynchronous hosts.
        /// </summary>
        class AsyncHostConsoleDispatcher : Dispatcher {
            Queue<InputLine> _buffer;
            bool _isExecuting;
            _Marshaler _marshaler;

            public AsyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher) {
                _marshaler = new _Marshaler(this);
            }

            bool IsStarted {
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

            void ProcessInputs() {
                if (_isExecuting) {
                    return;
                }

                if (_buffer.Count > 0) {
                    InputLine inputLine = _buffer.Dequeue();
                    Tuple<bool, bool> executeState = Process(inputLine);
                    if (executeState.Item1) {
                        _isExecuting = true;

                        if (!executeState.Item2) {
                            // If NOT really executed, processing the same as ExecuteEnd event
                            OnExecuteEnd();
                        }
                    }
                }
            }

            void OnExecuteEnd() {
                if (IsStarted) // Filter out noise. A host could execute private commands.
                {
                    Debug.Assert(_isExecuting);
                    _isExecuting = false;

                    PromptNewLine();
                    ProcessInputs();
                }
            }

            /// <summary>
            /// This private Marshaler marshals async host event to main thread so that the dispatcher
            /// doesn't need to worry about threading.
            /// </summary>
            class _Marshaler : Marshaler<AsyncHostConsoleDispatcher> {
                public _Marshaler(AsyncHostConsoleDispatcher impl)
                    : base(impl) {
                }

                public void AsyncHost_ExecuteEnd(object sender, EventArgs e) {
                    Invoke(() => _impl.OnExecuteEnd());
                }
            }
        }
    }

    [Flags]
    enum InputLineFlag {
        Echo = 1,
        Execute = 2
    }

    class InputLine {
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
