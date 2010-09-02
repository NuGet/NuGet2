using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using NuPackConsole.Implementation.Console;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace NuPackConsole.Implementation.PowerConsole
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(PowerConsoleWindow.ContentType)]
    [Name("PowerConsoleCompletion")]
    class CompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        public IWpfConsoleService WpfConsoleService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return WpfConsoleService.TryCreateCompletionSource(textBuffer) as ICompletionSource;
        }
    }
}
