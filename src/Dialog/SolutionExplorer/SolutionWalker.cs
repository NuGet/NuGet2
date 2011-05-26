using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog {
    internal static class SolutionWalker {
        public static ProjectNodeBase Walk(
            Solution solution,
            Func<Project, bool> checkedStateSelector,
            Func<Project, bool> enabledStateSelector) {
            if (solution == null) {
                throw new ArgumentNullException("solution");
            }

            if (!solution.IsOpen) {
                return null;
            }

            if (checkedStateSelector == null) {
                checkedStateSelector = p => false;
            }

            if (enabledStateSelector == null) {
                enabledStateSelector = p => true;
            }

            var children = CreateProjectNode(solution.Projects.OfType<Project>(), checkedStateSelector, enabledStateSelector).ToArray();
            Array.Sort(children, ProjectNodeComparer.Default);

            return new FolderNode(
                String.Format(CultureInfo.CurrentCulture, Resources.Dialog_SolutionNode, solution.GetName()),
                children);
        }

        private static IEnumerable<ProjectNodeBase> CreateProjectNode(
            IEnumerable<Project> projects,
            Func<Project, bool> checkedStateSelector,
            Func<Project, bool> enabledStateSelector) {

            foreach (var project in projects) {
                if (project.IsSupported()) {
                    yield return new ProjectNode(project) {
                        // default checked state of this node will be determined by the passed-in selector
                        IsSelected = checkedStateSelector(project),
                        IsEnabled = enabledStateSelector(project)
                    };
                }
                else if (project.IsSolutionFolder()) {
                    if (project.ProjectItems != null) {
                        var children = CreateProjectNode(
                            project.ProjectItems.
                                OfType<ProjectItem>().
                                Where(p => p.SubProject != null).
                                Select(p => p.SubProject),
                            checkedStateSelector,
                            enabledStateSelector
                        ).ToArray();

                        if (children.Length > 0) {
                            Array.Sort(children, ProjectNodeComparer.Default);
                            // only create a folder node if it has at least one child
                            yield return new FolderNode(project.Name, children);
                        }
                    }
                }
            }
        }

        private class ProjectNodeComparer : IComparer<ProjectNodeBase> {
            public static readonly ProjectNodeComparer Default = new ProjectNodeComparer();

            private ProjectNodeComparer() {
            }

            public int Compare(ProjectNodeBase first, ProjectNodeBase second) {
                if (first == null && second == null) {
                    return 0;
                }
                else if (first == null) {
                    return -1;
                }
                else if (second == null) {
                    return 1;
                }

                // solution folder goes before projects
                if (first is FolderNode && second is ProjectNode) {
                    return -1;
                }
                else if (first is ProjectNode && second is FolderNode) {
                    return 1;
                }
                else {
                    // if the two nodes are of the same kinds, compare by their names
                    return StringComparer.CurrentCultureIgnoreCase.Compare(first.Name, second.Name);
                }
            }
        }
    }
}