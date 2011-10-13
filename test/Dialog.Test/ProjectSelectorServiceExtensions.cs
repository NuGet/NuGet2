using System;
using System.Collections.Generic;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;

namespace NuGet.Dialog.Test
{
    internal static class WindowServiceExtensions
    {
        public static IEnumerable<Project> ShowProjectSelectorWindow(
            this IUserNotifierServices userNotifierServices,
            string instructionText,
            Predicate<Project> checkedStateSelector)
        {

            return userNotifierServices.ShowProjectSelectorWindow(
                instructionText,
                package: null,
                checkedStateSelector: checkedStateSelector,
                enabledStateSelector: ignore => true);
        }
    }
}
