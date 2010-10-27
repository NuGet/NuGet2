namespace NuGet.VisualStudio {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using EnvDTE;

    public static class ProjectExtensions {
        // List of project types
        // http://www.mztools.com/articles/2008/MZ2008017.aspx
        private static readonly string[] _supportedProjectTypes = new[] { VsConstants.WebSiteProjectKind, 
                                                                          VsConstants.CsharpProjectKind, 
                                                                          VsConstants.VbProjectKind };

        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar };
        // Get the ProjectItems for a folder path
        public static ProjectItems GetProjectItems(this Project project, string folderPath, bool createIfNotExists = false) {
            // Traverse the path to get at the directory
            string[] pathParts = folderPath.Split(PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Aggregate(project.ProjectItems, (projectItems, folderName) => GetOrCreateFolder(projectItems, folderName, createIfNotExists));
        }

        public static ProjectItem GetProjectItem(this Project project, string path) {
            string folderPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            ProjectItems container = GetProjectItems(project, folderPath);

            ProjectItem projectItem;
            // If we couldn't get the folder, or the file doesn't exist, return null
            if (container == null || !container.TryGetFile(fileName, out projectItem)) {
                return null;
            }

            return projectItem;
        }

        public static bool DeleteProjectItem(this Project project, string path) {
            ProjectItem projectItem = GetProjectItem(project, path);
            if (projectItem == null) {
                return false;
            }

            projectItem.Delete();
            return true;
        }

        public static bool TryGetFolder(this ProjectItems projectItems, string name, out ProjectItem projectItem) {
            return TryGetProjectItem(projectItems, name, new[] { VsConstants.VsProjectItemKindPhysicalFolder }, out projectItem);
        }

        public static bool TryGetFile(this ProjectItems projectItems, string name, out ProjectItem projectItem) {
            return TryGetProjectItem(projectItems, name, new[] { VsConstants.VsProjectItemKindPhysicalFile }, out projectItem);
        }

        public static bool TryGetProjectItem(this ProjectItems projectItems, string name, IEnumerable<string> kinds, out ProjectItem projectItem) {
            projectItem = GetProjectItem(projectItems, name);

            if (projectItem == null) {
                // If we didn't find the project item at the top level, then we look one more level down.
                // In VS files can have other nested files like aspx and aspx.cs. These are actually top level files in the file system
                // but are represented as nested project items in VS.
                projectItem = (from ProjectItem item in projectItems
                               where kinds.Contains(item.Kind) &&
                                     item.ProjectItems != null &&
                                     item.ProjectItems.Count > 0
                               select GetProjectItem(item.ProjectItems, name) into item
                               where item != null
                               select item).FirstOrDefault();
            }

            return projectItem != null;
        }

        private static ProjectItem GetProjectItem(ProjectItems projectItems, string name) {
            return (from ProjectItem item in projectItems
                    where item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    select item).FirstOrDefault();
        }

        public static IEnumerable<ProjectItem> GetChildItems(this Project project, string path, string filter, params string[] kinds) {
            ProjectItems projectItems = GetProjectItems(project, path);

            if (projectItems == null) {
                return Enumerable.Empty<ProjectItem>();
            }

            Regex matcher = GetFilterRegex(filter);

            return from ProjectItem p in projectItems
                   where kinds.Contains(p.Kind) && matcher.IsMatch(p.Name)
                   select p;
        }

        public static T GetPropertyValue<T>(this Project project, string propertyName) {
            Property property = project.Properties.Item(propertyName);
            if (property != null) {
                return (T)property.Value;
            }
            return default(T);
        }

        private static Regex GetFilterRegex(string wildcard) {
            string pattern = String.Join(String.Empty, wildcard.Split('.').Select(GetPattern));
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPattern(string token) {
            return token == "*" ? @"(.*)" : @"(" + token + ")";
        }

        private static ProjectItems GetOrCreateFolder(ProjectItems projectItems, string folderName, bool createIfNotExists) {
            if (projectItems == null) {
                return null;
            }

            ProjectItem subFolder;
            if (projectItems.TryGetFolder(folderName, out subFolder)) {
                // Get the sub folder
                return subFolder.ProjectItems;
            }
            else if (createIfNotExists) {
                Property property = projectItems.Parent.Properties.Item("FullPath");

                Debug.Assert(property != null, "Unable to get full path property from the project item");
                // Get the full path of this folder on disk and add it
                string fullPath = Path.Combine(property.Value, folderName);

                return projectItems.AddFromDirectory(fullPath).ProjectItems;
            }

            return null;
        }

        public static bool IsSupported(this Project project) {
            return project.Kind != null &&
                   _supportedProjectTypes.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsWebSite(this Project project) {
            return project.Kind != null && project.Kind.Equals(VsConstants.WebSiteProjectKind, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsUnloaded(this Project project) {
            return VsConstants.UnloadedProjectKind.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }

    }
}
