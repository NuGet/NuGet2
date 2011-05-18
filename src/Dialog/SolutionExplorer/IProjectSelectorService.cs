using System;
using System.Collections.Generic;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog {
    public interface IProjectSelectorService {
        IEnumerable<Project> ShowProjectSelectorWindow(Func<Project, bool> checkedStateSelector);
        void ShowSummaryWindow(object failedProjects);
    }
}