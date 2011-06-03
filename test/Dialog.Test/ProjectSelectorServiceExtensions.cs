using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.Dialog.Test {
    internal static class ProjectSelectorServiceExtensions {
        public static IEnumerable<Project> ShowProjectSelectorWindow(this IProjectSelectorService projectSelector, string instructionText, Func<Project, bool> checkedStateSelector) {
            return projectSelector.ShowProjectSelectorWindow(instructionText, checkedStateSelector, ignore => true);
        }
    }
}
