using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using VsWebSite;
using MsBuildProject = Microsoft.Build.Evaluation.Project;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

namespace NuGet.VisualStudio {
    public static class ProjectExtensions {
        private const string WebConfig = "web.config";
        private const string AppConfig = "app.config";
        private const string BinFolder = "Bin";

        private static readonly Dictionary<string, string> _knownNestedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "web.debug.config", "web.config" },
            { "web.release.config", "web.config" }
        };

        private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                                                                          VsConstants.WebSiteProjectTypeGuid, 
                                                                          VsConstants.CsharpProjectTypeGuid, 
                                                                          VsConstants.VbProjectTypeGuid,
                                                                          VsConstants.FsharpProjectTypeGuid,
                                                                          VsConstants.WixProjectTypeGuid };

        // List of project types that cannot have references added to them
        private static readonly string[] _unsupportedProjectTypesForAddingReferences = new[] { VsConstants.WixProjectTypeGuid };
        // List of project types that cannot have binding redirects added
        private static readonly string[] _unsupportedProjectTypesForBindingRedirects = new[] { VsConstants.WixProjectTypeGuid };

        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar };
        // Get the ProjectItems for a folder path
        public static ProjectItems GetProjectItems(this Project project, string folderPath, bool createIfNotExists = false) {
            // Traverse the path to get at the directory
            string[] pathParts = folderPath.Split(PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Aggregate(project.ProjectItems, (projectItems, folderName) => GetOrCreateFolder(projectItems, folderName, createIfNotExists));
        }

        public static ProjectItem GetProjectItem(this Project project, string path) {
            string folderPath = Path.GetDirectoryName(path);
            string itemName = Path.GetFileName(path);

            ProjectItems container = GetProjectItems(project, folderPath);

            ProjectItem projectItem;
            // If we couldn't get the folder, or the child item doesn't exist, return null
            if (container == null ||
                (!container.TryGetFile(itemName, out projectItem) &&
                 !container.TryGetFolder(itemName, out projectItem))) {
                return null;
            }

            return projectItem;
        }

        /// <summary>
        /// Recursively retrieves all supported child projects of a virtual folder.
        /// </summary>
        /// <param name="project">The root container project</param>
        public static IEnumerable<Project> GetSupportedChildProjects(this Project project) {
            if (!project.IsSolutionFolder()) {
                yield break;
            }

            var containerProjects = new Queue<Project>();
            containerProjects.Enqueue(project);

            while (containerProjects.Any()) {
                var containerProject = containerProjects.Dequeue();
                foreach (ProjectItem item in containerProject.ProjectItems) {
                    var nestedProject = item.SubProject;
                    if (nestedProject == null) {
                        continue;
                    }
                    else if (nestedProject.IsSupported()) {
                        yield return nestedProject;
                    }
                    else if (nestedProject.IsSolutionFolder()) {
                        containerProjects.Enqueue(nestedProject);
                    }
                }
            }
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
            projectItem = GetProjectItem(projectItems, name, VsConstants.VsProjectItemKindPhysicalFolder);

            return projectItem != null;
        }

        public static bool TryGetFile(this ProjectItems projectItems, string name, out ProjectItem projectItem) {
            projectItem = GetProjectItem(projectItems, name, VsConstants.VsProjectItemKindPhysicalFile);

            if (projectItem == null) {
                // Try to get the nested project item
                return TryGetFileNestedFile(projectItems, name, out projectItem);
            }

            return projectItem != null;
        }

        /// <summary>
        /// // If we didn't find the project item at the top level, then we look one more level down.
        /// In VS files can have other nested files like foo.aspx and foo.aspx.cs or web.config and web.debug.config. 
        /// These are actually top level files in the file system but are represented as nested project items in VS.            
        /// </summary>
        private static bool TryGetFileNestedFile(ProjectItems projectItems, string name, out ProjectItem projectItem) {
            string parentFileName;
            if (!_knownNestedFiles.TryGetValue(name, out parentFileName)) {
                parentFileName = Path.GetFileNameWithoutExtension(name);
            }

            // If it's not one of the known nested files then we're going to look up prefixes backwards
            // i.e. if we're looking for foo.aspx.cs then we look for foo.aspx then foo.aspx.cs as a nested file
            ProjectItem parentProjectItem = GetProjectItem(projectItems, parentFileName, VsConstants.VsProjectItemKindPhysicalFile);

            if (parentProjectItem != null) {
                // Now try to find the nested file
                projectItem = GetProjectItem(parentProjectItem.ProjectItems, name, VsConstants.VsProjectItemKindPhysicalFile);
            }
            else {
                projectItem = null;
            }

            return projectItem != null;
        }

        public static bool SupportsConfig(this Project project) {
            return !IsClassLibrary(project);
        }

        private static bool IsClassLibrary(this Project project) {
            if (project.IsWebSite()) {
                return false;
            }

            // Consider class libraries projects that have one project type guid and an output type of project library.
            var outputType = project.GetPropertyValue<prjOutputType>("OutputType");
            return project.GetProjectTypeGuids().Count() == 1 &&
                   outputType == prjOutputType.prjOutputTypeLibrary;
        }

        // TODO: Return null for library projects
        public static string GetConfigurationFile(this Project project) {
            return project.IsWebProject() ? WebConfig : AppConfig;
        }

        private static ProjectItem GetProjectItem(ProjectItems projectItems, string name, string kind) {
            try {
                ProjectItem projectItem = projectItems.Item(name);
                if (kind.Equals(projectItem.Kind, StringComparison.OrdinalIgnoreCase)) {
                    return projectItem;
                }
            }
            catch {
            }

            return null;
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

        public static string GetFullPath(this Project project) {
            return project.GetPropertyValue<string>("FullPath");
        }

        public static T GetPropertyValue<T>(this Project project, string propertyName) {
            try {
                Property property = project.Properties.Item(propertyName);
                if (property != null) {
                    // REVIEW: Should this cast or convert?
                    return (T)property.Value;
                }
            }
            catch (ArgumentException) {

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

                try {
                    return projectItems.AddFromDirectory(fullPath).ProjectItems;
                }
                catch (NotImplementedException) {
                    // This is the case for F#'s project system, we can't add from directory so we fall back
                    // to this impl
                    return projectItems.AddFolder(folderName).ProjectItems;
                }
            }

            return null;
        }

        public static bool IsWebProject(this Project project) {
            var types = new HashSet<string>(project.GetProjectTypeGuids(), StringComparer.OrdinalIgnoreCase);
            return types.Contains(VsConstants.WebSiteProjectTypeGuid) || types.Contains(VsConstants.WebApplicationProjectTypeGuid);
        }

        public static bool IsWebSite(this Project project) {
            return project.Kind != null && project.Kind.Equals(VsConstants.WebSiteProjectTypeGuid, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSupported(this Project project) {
            return project.Kind != null && _supportedProjectTypes.Contains(project.Kind);
        }

        public static bool IsSolutionFolder(this Project project) {
            return project.Kind != null && project.Kind.Equals(VsConstants.VsProjectItemKindSolutionFolder, StringComparison.OrdinalIgnoreCase);
        }

        public static bool SupportsReferences(this Project project) {
            return project.Kind != null &&
                !_unsupportedProjectTypesForAddingReferences.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public static bool SupportsBindingRedirects(this Project project) {
            return project.Kind != null &
                !_unsupportedProjectTypesForBindingRedirects.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsUnloaded(this Project project) {
            return VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetOutputPath(this Project project) {
            // For Websites the output path is the bin folder
            string outputPath = project.IsWebSite() ? BinFolder : project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            return Path.Combine(project.GetFullPath(), outputPath);
        }

        public static IVsHierarchy ToVsHierarchy(this Project project) {
            IVsHierarchy hierarchy;

            // Get the vs solution
            IVsSolution solution = ServiceLocator.GetInstance<IVsSolution>();
            int hr = solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

            if (hr != VsConstants.S_OK) {
                Marshal.ThrowExceptionForHR(hr);
            }

            return hierarchy;
        }

        public static IVsProjectBuildSystem ToVsProjectBuildSystem(this Project project) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }
            // Convert the project to an IVsHierarchy and see if it implements IVsProjectBuildSystem
            return project.ToVsHierarchy() as IVsProjectBuildSystem;
        }

        public static IEnumerable<string> GetProjectTypeGuids(this Project project) {
            // Get the vs hierarchy as an IVsAggregatableProject to get the project type guids
            var aggregatableProject = (IVsAggregatableProject)project.ToVsHierarchy();

            string projectTypeGuids;
            int hr = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);

            if (hr != VsConstants.S_OK) {
                Marshal.ThrowExceptionForHR(hr);
            }

            return projectTypeGuids.Split(';');
        }

        internal static IEnumerable<Project> GetReferencedProjects(this Project project) {
            if (project.IsWebSite()) {
                return GetWebsiteReferencedProjects(project);
            }

            var projects = new List<Project>();
            References references = project.Object.References;
            foreach (Reference reference in references) {
                // Get the referenced project from the reference if any
                if (reference.SourceProject != null) {
                    projects.Add(reference.SourceProject);
                }
            }
            return projects;
        }

        internal static HashSet<string> GetAssemblyClosure(this Project project, IDictionary<string, HashSet<string>> visitedProjects) {
            HashSet<string> assemblies;
            if (visitedProjects.TryGetValue(project.UniqueName, out assemblies)) {
                return assemblies;
            }

            assemblies = new HashSet<string>(PathComparer.Default);
            assemblies.AddRange(GetLocalProjectAssemblies(project));
            assemblies.AddRange(project.GetReferencedProjects().SelectMany(p => GetAssemblyClosure(p, visitedProjects)));

            visitedProjects.Add(project.UniqueName, assemblies);

            return assemblies;
        }

        private static IEnumerable<Project> GetWebsiteReferencedProjects(Project project) {
            var projects = new List<Project>();
            AssemblyReferences references = project.Object.References;
            foreach (AssemblyReference reference in references) {
                if (reference.ReferencedProject != null) {
                    projects.Add(reference.ReferencedProject);
                }
            }
            return projects;
        }

        private static HashSet<string> GetLocalProjectAssemblies(Project project) {
            if (project.IsWebSite()) {
                return GetWebsiteLocalAssemblies(project);
            }

            var assemblies = new HashSet<string>(PathComparer.Default);
            References references = project.Object.References;
            foreach (Reference reference in references) {
                // Get the referenced project from the reference if any
                if (reference.SourceProject == null &&
                    reference.CopyLocal &&
                    File.Exists(reference.Path)) {
                    assemblies.Add(reference.Path);
                }
            }
            return assemblies;
        }

        private static HashSet<string> GetWebsiteLocalAssemblies(Project project) {
            var assemblies = new HashSet<string>(PathComparer.Default);
            AssemblyReferences references = project.Object.References;
            foreach (AssemblyReference reference in references) {
                // For websites only include bin assemblies
                if (reference.ReferencedProject == null &&
                    reference.ReferenceKind == AssemblyReferenceType.AssemblyReferenceBin &&
                    File.Exists(reference.FullPath)) {
                    assemblies.Add(reference.FullPath);
                }
            }
            return assemblies;
        }

        public static MsBuildProject AsMSBuildProject(this Project project) {
            return ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.FullName).FirstOrDefault();
        }

        /// <summary>
        /// Returns the unique name of the specified project including all solution folder names containing it.
        /// </summary>
        /// <remarks>
        /// This is different from the DTE Project.UniqueName property, which is the absolute path to the project file.
        /// </remarks>
        public static string GetCustomUniqueName(this Project project) {
            if (project.IsWebSite()) {
                // website projects always have unique name
                return project.Name;
            }
            else {
                Stack<string> nameParts = new Stack<string>();

                Project cursor = project;
                nameParts.Push(cursor.Name);

                // walk up till the solution root
                while (cursor.ParentProjectItem != null && cursor.ParentProjectItem.ContainingProject != null) {
                    cursor = cursor.ParentProjectItem.ContainingProject;
                    nameParts.Push(cursor.Name);
                }

                return String.Join("\\", nameParts);
            }
        }

        private class PathComparer : IEqualityComparer<string> {
            public static readonly PathComparer Default = new PathComparer();
            public bool Equals(string x, string y) {
                return Path.GetFileName(x).Equals(Path.GetFileName(y));
            }

            public int GetHashCode(string obj) {
                return Path.GetFileName(obj).GetHashCode();
            }
        }
    }
}