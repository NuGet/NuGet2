using System;
using System.Collections.Generic;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;

namespace NuGet.Dialog.Test {
    internal static class WindowServiceExtensions {
        public static IEnumerable<Project> ShowProjectSelectorWindow(this IWindowServices windowServices, string instructionText, Func<Project, bool> checkedStateSelector) {
            return windowServices.ShowProjectSelectorWindow(instructionText, checkedStateSelector, ignore => true);
        }
    }
}
