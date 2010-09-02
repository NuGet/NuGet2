using System.ComponentModel.Composition;
using NuPackConsole.Implementation.Console;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace NuPackConsole.Implementation.PowerConsole
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(PowerConsoleWindow.ContentType)]
    class ClassifierProvider : IClassifierProvider
    {
        [Import]
        public IWpfConsoleService WpfConsoleService { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return WpfConsoleService.GetClassifier(textBuffer) as IClassifier;
        }
    }
}
