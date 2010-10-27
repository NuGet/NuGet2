using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using EditorDefGuidList = Microsoft.VisualStudio.Editor.DefGuidList;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;

namespace NuGetConsole.Implementation.Console {
    interface IPrivateWpfConsole : IWpfConsole {
        SnapshotPoint? InputLineStart { get; }
        void BeginInputLine();
        SnapshotSpan? EndInputLine(bool isEcho);
        InputHistory InputHistory { get; }
    }

    class WpfConsole : ObjectWithFactory<WpfConsoleService> {
        IServiceProvider ServiceProvider { get; set; }
        public string ContentTypeName { get; private set; }
        public string HostName { get; private set; }

        public event EventHandler<EventArgs<Tuple<SnapshotSpan, Color?, Color?>>> NewColorSpan;
        public event EventHandler ConsoleCleared;

        public WpfConsole(WpfConsoleService factory, IServiceProvider sp, string contentTypeName, string hostName)
            : base(factory) {
            UtilityMethods.ThrowIfArgumentNull(sp);

            this.ServiceProvider = sp;
            this.ContentTypeName = contentTypeName;
            this.HostName = hostName;
        }

        IPrivateConsoleDispatcher _dispatcher;
        public IPrivateConsoleDispatcher Dispatcher {
            get {
                if (_dispatcher == null) {
                    _dispatcher = new ConsoleDispatcher(Marshaler);
                }
                return _dispatcher;
            }
        }

        IOleServiceProvider OleServiceProvider {
            get {
                return ServiceProvider.GetService<IOleServiceProvider>(typeof(IOleServiceProvider));
            }
        }

        IContentType _contentType;
        IContentType ContentType {
            get {
                if (_contentType == null) {
                    _contentType = Factory.ContentTypeRegistryService.GetContentType(this.ContentTypeName);
                    if (_contentType == null) {
                        _contentType = Factory.ContentTypeRegistryService.AddContentType(
                            this.ContentTypeName, new string[] { "text" });
                    }
                }

                return _contentType;
            }
        }

        IVsTextBuffer _bufferAdapter;
        IVsTextBuffer VsTextBuffer {
            get {
                if (_bufferAdapter == null) {
                    _bufferAdapter = Factory.VsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(OleServiceProvider, ContentType);
                    _bufferAdapter.InitializeContent(string.Empty, 0);
                }

                return _bufferAdapter;
            }
        }

        IWpfTextView _wpfTextView;
        public IWpfTextView WpfTextView {
            get {
                if (_wpfTextView == null) {
                    _wpfTextView = Factory.VsEditorAdaptersFactoryService.GetWpfTextView(VsTextView);
                }

                return _wpfTextView;
            }
        }

        IWpfTextViewHost WpfTextViewHost {
            get {
                IVsUserData userData = VsTextView as IVsUserData;
                object data;
                Guid guidIWpfTextViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidIWpfTextViewHost, out data);
                IWpfTextViewHost wpfTextViewHost = data as IWpfTextViewHost;

                return wpfTextViewHost;
            }
        }


        enum ReadOnlyRegionType {
            /// <summary>
            /// No ReadOnly region. The whole text buffer allows edit.
            /// </summary>
            None,

            /// <summary>
            /// Begin and body are ReadOnly. Only allows edit at the end.
            /// </summary>
            BeginAndBody,

            /// <summary>
            /// The whole text buffer is ReadOnly. Does not allow any edit.
            /// </summary>
            All
        };

        IReadOnlyRegion _readOnlyRegionBegin;
        IReadOnlyRegion _readOnlyRegionBody;


        private void SetReadOnlyRegionType(ReadOnlyRegionType value) {
            ITextBuffer buffer = WpfTextView.TextBuffer;
            ITextSnapshot snapshot = buffer.CurrentSnapshot;

            using (IReadOnlyRegionEdit edit = buffer.CreateReadOnlyRegionEdit()) {
                edit.ClearReadOnlyRegion(ref _readOnlyRegionBegin);
                edit.ClearReadOnlyRegion(ref _readOnlyRegionBody);

                switch (value) {
                    case ReadOnlyRegionType.BeginAndBody:
                        if (snapshot.Length > 0) {
                            _readOnlyRegionBegin = edit.CreateReadOnlyRegion(new Span(0, 0), SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny);
                            _readOnlyRegionBody = edit.CreateReadOnlyRegion(new Span(0, snapshot.Length));
                        }
                        break;

                    case ReadOnlyRegionType.All:
                        _readOnlyRegionBody = edit.CreateReadOnlyRegion(new Span(0, snapshot.Length), SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny);
                        break;
                }

                edit.Apply();
            }
        }

        SnapshotPoint? _inputLineStart;

        /// <summary>
        /// Get current input line start point (updated to current WpfTextView's text snapshot).
        /// </summary>
        public SnapshotPoint? InputLineStart {
            get {
                if (_inputLineStart != null) {
                    ITextSnapshot snapshot = WpfTextView.TextSnapshot;
                    if (_inputLineStart.Value.Snapshot != snapshot) {
                        _inputLineStart = _inputLineStart.Value.TranslateTo(snapshot, PointTrackingMode.Negative);
                    }
                }
                return _inputLineStart;
            }
        }

        public SnapshotSpan GetInputLineExtent(int start = 0, int length = -1) {
            Debug.Assert(_inputLineStart != null);

            SnapshotPoint beginPoint = InputLineStart.Value + start;
            return length >= 0 ?
                new SnapshotSpan(beginPoint, length) :
                new SnapshotSpan(beginPoint, beginPoint.GetContainingLine().End);
        }

        public SnapshotSpan InputLineExtent {
            get {
                return GetInputLineExtent();
            }
        }

        /// <summary>
        /// Get the snapshot extent from InputLineStart to END. Normally this console expects
        /// one line only on InputLine. However in some cases multiple lines could appear, e.g.
        /// when a DTE event handler writes to the console. This scenario is not fully supported,
        /// but it is better to clean up nicely with ESC/ArrowUp/Return.
        /// </summary>
        public SnapshotSpan AllInputExtent {
            get {
                SnapshotPoint start = InputLineStart.Value;
                return new SnapshotSpan(start, start.Snapshot.GetEnd());
            }
        }

        public string InputLineText {
            get {
                return InputLineExtent.GetText();
            }
        }

        public void BeginInputLine() {
            if (_inputLineStart == null) {
                SetReadOnlyRegionType(ReadOnlyRegionType.BeginAndBody);
                _inputLineStart = WpfTextView.TextSnapshot.GetEnd();
            }
        }

        public SnapshotSpan? EndInputLine(bool isEcho = false) {
            // Reset history navigation upon end of a command line
            ResetNavigateHistory();

            if (_inputLineStart != null) {
                SnapshotSpan inputSpan = InputLineExtent;

                _inputLineStart = null;
                SetReadOnlyRegionType(ReadOnlyRegionType.All);
                if (!isEcho) {
                    Dispatcher.PostInputLine(new InputLine(inputSpan));
                }

                return inputSpan;
            }

            return null;
        }

        #region Marshaler

        _Marshaler _marshaler;
        _Marshaler Marshaler {
            get {
                if (_marshaler == null) {
                    _marshaler = new _Marshaler(this);
                }
                return _marshaler;
            }
        }

        public IWpfConsole MarshalledConsole {
            get { return this.Marshaler; }
        }

        class _Marshaler : Marshaler<WpfConsole>, IWpfConsole, IPrivateWpfConsole {
            public _Marshaler(WpfConsole impl)
                : base(impl) {
            }

            #region IConsole
            public IHost Host {
                get { return Invoke(() => _impl.Host); }
                set { Invoke(() => { _impl.Host = value; }); }
            }

            public IConsoleDispatcher Dispatcher {
                get { return Invoke(() => _impl.Dispatcher); }
            }

            public int ConsoleWidth {
                get { return Invoke(() => _impl.ConsoleWidth); }
            }

            public void Write(string text) {
                Invoke(() => _impl.Write(text));
            }

            public void WriteLine(string text) {
                Invoke(() => _impl.WriteLine(text));
            }

            public void Write(string text, Color? foreground, Color? background) {
                Invoke(() => _impl.Write(text, foreground, background));
            }

            public void Clear() {
                Invoke(() => _impl.Clear());
            }
            #endregion

            #region IWpfConsole
            public object Content {
                get { return Invoke(() => _impl.Content); }
            }

            public object VsTextView {
                get { return Invoke(() => _impl.VsTextView); }
            }
            #endregion

            #region IPrivateWpfConsole
            public SnapshotPoint? InputLineStart {
                get { return Invoke(() => _impl.InputLineStart); }
            }

            public void BeginInputLine() {
                Invoke(() => _impl.BeginInputLine());
            }

            public SnapshotSpan? EndInputLine(bool isEcho) {
                return Invoke(() => _impl.EndInputLine(isEcho));
            }

            public InputHistory InputHistory {
                get { return Invoke(() => _impl.InputHistory); }
            }
            #endregion
        }

        #endregion

        #region IConsole
        IHost _host;
        public IHost Host {
            get {
                return _host;
            }
            set {
                if (_host != null) {
                    throw new InvalidOperationException();
                }
                _host = value;
            }
        }

        int _consoleWidth = -1;
        public int ConsoleWidth {
            get {
                if (_consoleWidth < 0) {
                    ITextViewMargin leftMargin = WpfTextViewHost.GetTextViewMargin(PredefinedMarginNames.Left);
                    ITextViewMargin rightMargin = WpfTextViewHost.GetTextViewMargin(PredefinedMarginNames.Right);

                    double marginSize = 0.0;
                    if (leftMargin != null && leftMargin.Enabled) {
                        marginSize += leftMargin.MarginSize;
                    }
                    if (rightMargin != null && rightMargin.Enabled) {
                        marginSize += rightMargin.MarginSize;
                    }

                    int n = (int)((WpfTextView.ViewportWidth - marginSize) / WpfTextView.FormattedLineSource.ColumnWidth);
                    _consoleWidth = Math.Max(80, n); // Larger of 80 or n
                }
                return _consoleWidth;
            }
        }

        void ResetConsoleWidth() {
            _consoleWidth = -1;
        }

        public void Write(string text) {
            if (_inputLineStart == null) // If not in input mode, need unlock to enable output
            {
                SetReadOnlyRegionType(ReadOnlyRegionType.None);
            }

            // Append text to editor buffer
            ITextBuffer textBuffer = WpfTextView.TextBuffer;
            textBuffer.Insert(textBuffer.CurrentSnapshot.Length, text);

            // Ensure caret visible (scroll)
            WpfTextView.Caret.EnsureVisible();

            if (_inputLineStart == null) // If not in input mode, need lock again
            {
                SetReadOnlyRegionType(ReadOnlyRegionType.All);
            }
        }

        public void WriteLine(string text) {
            // If append \n only, text becomes 1 line when copied to notepad.
            Write(text + Environment.NewLine);
        }

        public void Write(string text, Color? foreground, Color? background) {
            int begin = WpfTextView.TextSnapshot.Length;
            Write(text);
            int end = WpfTextView.TextSnapshot.Length;

            if (foreground != null || background != null) {
                SnapshotSpan span = new SnapshotSpan(WpfTextView.TextSnapshot, begin, end - begin);
                NewColorSpan.Raise(this, Tuple.Create(span, foreground, background));
            }
        }

        InputHistory _inputHistory;
        InputHistory InputHistory {
            get {
                if (_inputHistory == null) {
                    _inputHistory = new InputHistory();
                }
                return _inputHistory;
            }
        }

        IList<string> _historyInputs;
        int _currentHistoryInputIndex;

        void ResetNavigateHistory() {
            _historyInputs = null;
            _currentHistoryInputIndex = -1;
        }

        public void NavigateHistory(int offset) {
            if (_historyInputs == null) {
                _historyInputs = InputHistory.History;
                if (_historyInputs == null) {
                    _historyInputs = new string[] { };
                }

                _currentHistoryInputIndex = _historyInputs.Count;
            }

            int index = _currentHistoryInputIndex + offset;
            if (index >= -1 && index <= _historyInputs.Count) {
                _currentHistoryInputIndex = index;
                string input = (index >= 0 && index < _historyInputs.Count) ? _historyInputs[_currentHistoryInputIndex] : string.Empty;

                // Replace all text after InputLineStart with new text
                WpfTextView.TextBuffer.Replace(AllInputExtent, input);
                WpfTextView.Caret.EnsureVisible();
            }
        }

        public void Clear() {
            SetReadOnlyRegionType(ReadOnlyRegionType.None);

            ITextBuffer textBuffer = WpfTextView.TextBuffer;
            textBuffer.Delete(new Span(0, textBuffer.CurrentSnapshot.Length));

            // Dispose existing incompleted input line
            _inputLineStart = null;

            // Raise event
            ConsoleCleared.Raise(this);
        }
 
        public void ClearConsole() {
            if (_inputLineStart != null) {
                Dispatcher.ClearConsole();
            }
        }


        #endregion

        #region IWpfConsole

        IVsTextView _view;
        public IVsTextView VsTextView {
            get {
                if (_view == null) {
                    _view = Factory.VsEditorAdaptersFactoryService.CreateVsTextViewAdapter(OleServiceProvider);
                    _view.Initialize(
                        VsTextBuffer as IVsTextLines,
                        IntPtr.Zero,
                        (uint)(TextViewInitFlags.VIF_HSCROLL | TextViewInitFlags.VIF_VSCROLL) | (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                        null);

                    // Set font and color
                    IVsTextEditorPropertyCategoryContainer propCategoryContainer = _view as IVsTextEditorPropertyCategoryContainer;
                    if (propCategoryContainer != null) {
                        IVsTextEditorPropertyContainer propContainer;
                        Guid guidPropCategory = EditorDefGuidList.guidEditPropCategoryViewMasterSettings;
                        propCategoryContainer.GetPropertyCategory(ref guidPropCategory, out propContainer);
                        propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewGeneral_FontCategory, EditorDefGuidList.guidCommandWindowFontCategory);
                        propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewGeneral_ColorCategory, EditorDefGuidList.guidCommandWindowFontCategory);
                    }

                    // add myself as IConsole
                    WpfTextView.TextBuffer.Properties.AddProperty(typeof(IConsole), this);

                    // Initial mark readonly region. Must call Start() to start accepting inputs.
                    SetReadOnlyRegionType(ReadOnlyRegionType.All);

                    // Set some EditorOptions: -DragDropEditing, +WordWrap
                    IEditorOptions editorOptions = Factory.EditorOptionsFactoryService.GetOptions(WpfTextView);
                    editorOptions.SetOptionValue(DefaultTextViewOptions.DragDropEditingId, false);
                    editorOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap);

                    // Reset console width when needed
                    WpfTextView.ViewportWidthChanged += (sender, e) => ResetConsoleWidth();
                    WpfTextView.ZoomLevelChanged += (sender, e) => ResetConsoleWidth();

                    // Create my Command Filter
                    new WpfConsoleKeyProcessor(this);
                }

                return _view;
            }
        }

        public object Content {
            get {
                return WpfTextViewHost.HostControl;
            }
        }

        #endregion
    }
}
