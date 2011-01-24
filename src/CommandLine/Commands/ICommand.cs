using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace NuGet {

    [InheritedExport]
    public interface ICommand {
        List<string> Arguments { get; set; }
        void Execute();
    }
}
