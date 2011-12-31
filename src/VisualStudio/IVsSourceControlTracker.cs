using System;

namespace NuGet.VisualStudio
{
    public interface IVsSourceControlTracker
    {
        event EventHandler SolutionBoundToSourceControl;
    }
}
