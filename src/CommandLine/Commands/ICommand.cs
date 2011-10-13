using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace NuGet
{
    [InheritedExport]
    public interface ICommand
    {
        CommandAttribute CommandAttribute { get; }

        IList<string> Arguments { get; }

        void Execute();
    }
}
