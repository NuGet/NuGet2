using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.Dialog.PackageManagerUI
{
    public interface IUserNotifierServices
    {
        bool ShowLicenseWindow(IEnumerable<IPackage> packages);
        IEnumerable<Project> ShowProjectSelectorWindow(
            string instructionText,
            IPackage package,
            Predicate<Project> checkedStateSelector,
            Predicate<Project> enabledStateSelector);
        void ShowSummaryWindow(object failedProjects);
        bool? ShowRemoveDependenciesWindow(string message);
        FileConflictResolution ShowFileConflictResolution(string message);
    }
}