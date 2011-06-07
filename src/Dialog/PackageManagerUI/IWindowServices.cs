using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.Dialog.PackageManagerUI {
    public interface IWindowServices {
        bool ShowLicenseWindow(IEnumerable<IPackage> packages);
        IEnumerable<Project> ShowProjectSelectorWindow(string instructionText, Func<Project, bool> checkedStateSelector, Func<Project, bool> enabledStateSelector);
        void ShowSummaryWindow(object failedProjects);
        bool AskToRemoveDependencyPackages(string message);
    }
}