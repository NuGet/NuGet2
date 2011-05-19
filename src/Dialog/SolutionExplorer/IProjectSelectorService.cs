using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.Dialog {
    public interface IProjectSelectorService {
        IEnumerable<Project> ShowProjectSelectorWindow(Func<Project, bool> checkedStateSelector);
        void ShowSummaryWindow(object failedProjects);
    }
}