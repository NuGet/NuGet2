using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IVsPackageManager : IPackageManager
    {
        IProjectManager GetProjectManager(Project project);
        ISolutionManager SolutionManager { get; }
    }
}