using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGetConsole.Implementation.Console;
using System.Windows;
using NuGet.VisualStudio;
using System.Windows.Controls;
using NuGetConsole.DebugConsole;
using System.Windows.Media;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;
using NuGet;

namespace NuGetConsole.Implementation
{
    [Guid("c01ab4c3-68be-4add-b51d-26fa1d26245f")]
    public sealed class DebugConsoleToolWindow : ToolWindowPane
    {
        private IWpfConsoleService _consoleService;
        private DebugWindow _debugWindow;
        private IWpfConsole _console;
        private bool _active;
        private IEnumerable<IDebugConsoleController> _sources;
        private EventHandler<DebugConsoleMessageEventArgs> _handler;

        public const string ContentType = "PackageManagerDebugConsole";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.VisualStudio.Shell.ToolWindowPane.set_Caption(System.String)")]
        public DebugConsoleToolWindow()
            : base(null)
        {
            // TODO: put this in the resources
            this.Caption = "Package Manager Debug Console";
            this.BitmapResourceID = 301;
            this.BitmapIndex = 0;

            _active = false;

            _handler = new EventHandler<DebugConsoleMessageEventArgs>(HandleMessage);
            _debugWindow = new DebugWindow();
            this.Content = _debugWindow;
        }

        public override void OnToolWindowCreated()
        {
            var consoleService = ServiceLocator.GetInstance<IWpfConsoleService>();

            CreateConsole(consoleService);

            _active = true;

            base.OnToolWindowCreated();
        }

        private IEnumerable<IDebugConsoleController> MessageSources
        {
            get
            {
                if (_sources == null)
                {
                    var source = ServiceLocator.GetInstance<IDebugConsoleController>();

                    List<IDebugConsoleController> sources = new List<IDebugConsoleController>();

                    if (source != null)
                    {
                        sources.Add(source);
                    }

                    _sources = sources;
                }

                return _sources;
            }
        }

        public void CreateConsole(IWpfConsoleService consoleService)
        {
            _consoleService = consoleService;

            _console = _consoleService.CreateConsole(ServiceLocator.PackageServiceProvider, DebugConsoleToolWindow.ContentType, "nugetdebug");

            _console.StartWritingOutput();

            IVsTextView view = _console.VsTextView as IVsTextView;

            var adapterFactory = ServiceLocator.GetInstance<IVsEditorAdaptersFactoryService>();

            IWpfTextView wpfView = adapterFactory.GetWpfTextView(view);

            // adjust the view options
            if (wpfView != null)
            {
                wpfView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.None);

                Brush blackBg = Brushes.Black;
                blackBg.Freeze();

                wpfView.Background = blackBg;
            }

            UIElement element = _console.Content as UIElement;
            _debugWindow.DebugGrid.Children.Add(element);

            // Add message sources
            AttachEvents();
        }

        public void Log(string message, ConsoleColor color)
        {
            if (IsActive)
            {
                _console.Write(message + Environment.NewLine, ConvertColor(color), null);
            }
        }

        private static Color ConvertColor(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Red:
                    return Colors.Red;
                case ConsoleColor.Blue:
                    return Colors.Blue;
                case ConsoleColor.Cyan:
                    return Colors.Cyan;
                case ConsoleColor.DarkBlue:
                    return Colors.DarkBlue;
                case ConsoleColor.DarkCyan:
                    return Colors.DarkCyan;
                case ConsoleColor.DarkGray:
                    return Colors.DarkGray;
                case ConsoleColor.DarkGreen:
                    return Colors.DarkGreen;
                case ConsoleColor.DarkMagenta:
                    return Colors.DarkMagenta;
                case ConsoleColor.DarkRed:
                    return Colors.DarkRed;
                case ConsoleColor.DarkYellow:
                    return Colors.DarkOrange;
                case ConsoleColor.Gray:
                    return Colors.Gray;
                case ConsoleColor.Green:
                    return Colors.Green;
                case ConsoleColor.Magenta:
                    return Colors.Magenta;
                case ConsoleColor.White:
                    return Colors.White;
                case ConsoleColor.Yellow:
                    return Colors.Yellow;

                default:
                    return Colors.White;
            }
        }

        public bool IsActive
        {
            get
            {
                return _active;
            }
        }

        private void AttachEvents()
        {
            foreach (var source in MessageSources)
            {
                source.OnMessage += _handler;
            }
        }

        private void DetachEvents()
        {
            foreach (var source in MessageSources)
            {
                source.OnMessage -= _handler;
            }
        }

        private void HandleMessage(object sender, DebugConsoleMessageEventArgs args)
        {
            Log(args.Message, args.Color);
        }

        protected override void Dispose(bool disposing)
        {
            DetachEvents();

            base.Dispose(disposing);
        }
    }
}
