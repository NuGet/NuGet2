using System;

namespace NuGet.VisualStudio
{
    public interface IVsCommonOperations
    {
        bool OpenFile(string filePath);
        IDisposable SaveSolutionExplorerNodeStates(ISolutionManager solutionManager);
    }
}