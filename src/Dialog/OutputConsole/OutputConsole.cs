using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGetConsole;

namespace NuGet.OutputWindowConsole {

    /// <summary>
    /// This class implements the IConsole interface in order to integrate with the PowerShellHost.
    /// It sends PowerShell host outputs to the VS Output tool window.
    /// </summary>
    internal class OutputConsole : IConsole, IConsoleDispatcher {

        // guid for our Output window pane
        private static Guid _outputWindowPaneGuid = new Guid("CEC55EC8-CC51-40E7-9243-57B87A6F6BEB");
        private const string _outputWindowPaneName = "Package Manager";

        private IVsOutputWindow _outputWindow;
        private IVsOutputWindowPane _outputWindowPane;
        private IVsUIShell _vsUIShell;

        public OutputConsole(IVsOutputWindow outputWindow, IVsUIShell vsUIShell) {
            if (outputWindow == null) {
                throw new ArgumentNullException("outputWindow");
            }
            if (vsUIShell == null) {
                throw new ArgumentNullException("vsUIShell");
            }
            _outputWindow = outputWindow;
            _vsUIShell = vsUIShell;
        }

        public IHost Host {
            get;
            set;
        }

        public IConsoleDispatcher Dispatcher {
            get { return this; }
        }

        public int ConsoleWidth {
            get {
                return 120;
            }
        }

        private void EnsureStarted() {
            if (_outputWindowPane == null) {
                Start();
            }
        }

        public void Write(string text) {
            if (String.IsNullOrEmpty(text)) {
                return;
            }

            EnsureStarted();

            foreach (string s in WrapText(text)) {
                _outputWindowPane.OutputStringThreadSafe(s);
            }
        }

        public void WriteLine(string text) {
            Write(text + Environment.NewLine);
        }

        public void Write(string text, System.Windows.Media.Color? foreground, System.Windows.Media.Color? background) {
            // the Output window doesn't allow setting text color
            Write(text);
        }

        public void Clear() {
            EnsureStarted();
            _outputWindowPane.Clear();
        }

        public void Start() {
            if (_outputWindowPane == null) {
                // create the Package Manager pane within the Output window
                int result = _outputWindow.CreatePane(ref _outputWindowPaneGuid, _outputWindowPaneName, fInitVisible: 1, fClearWithSolution: 0);
                if (result == VSConstants.S_OK) {
                    result = _outputWindow.GetPane(ref _outputWindowPaneGuid, out _outputWindowPane);

                    System.Diagnostics.Debug.Assert(result ==  VSConstants.S_OK);
                    System.Diagnostics.Debug.Assert(_outputWindowPane != null);
                }
            }

            // try to open the Output tool window, and activate our Package Manager pane
            IVsWindowFrame outputWindowFrame;
            Guid outputWindowGuid = new Guid(ToolWindowGuids.Outputwindow);
            int findResult = _vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref outputWindowGuid, out outputWindowFrame);
            if (findResult == VSConstants.S_OK && outputWindowFrame != null) {
                outputWindowFrame.Show();
                _outputWindowPane.Activate();
            }
        }

        public void ClearConsole() {
            Clear();
        }

        /// <summary>
        /// Break the string into multiple lines, each line doesn't exceed ConsoleWidth value
        /// </summary>
        /// <param name="text"></param>
        /// <returns>collection of strings, each corresponding to one line</returns>
        private IEnumerable<string> WrapText(string text) {
            string[] words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < words.Length; ++i) {
                if (builder.Length > 0) {
                    builder.Append(' ');
                }
                builder.Append(words[i]);

                if (i + 1 == words.Length || builder.Length + words[i + 1].Length > ConsoleWidth) {
                    if (i + 1 < words.Length) {
                        builder.AppendLine();
                    }
                    yield return builder.ToString();
                    builder.Clear();
                }
            }
        }
    }
}