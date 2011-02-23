using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace NuGet {

    [InheritedExport]
    public interface ICommand {
        CommandAttribute CommandAttribute { get; }
        List<string> Arguments { get; set; }
        void Execute();
    }
}
