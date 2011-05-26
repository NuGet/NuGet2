using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.Dialog {
    public interface IProjectSelectorService {
        IEnumerable<Project> ShowProjectSelectorWindow(Func<Project, bool> checkedStateSelector, Func<Project, bool> enabledStateSelector);
        void ShowSummaryWindow(object failedProjects);
    }
}