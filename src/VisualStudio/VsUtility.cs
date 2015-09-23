using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio
{
    // The class is also added as a link to VsEvents project
    // Don't add any code in this class that will result in loading of NuGet.VisualStudio.dll from VsEvents
    public static class VsUtility
    {
        public const string NuGetSolutionSettingsFolder = ".nuget";
        public const string PackageReferenceFile = "packages.config";

        private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        {
            VsConstants.WebSiteProjectTypeGuid, 
            VsConstants.CsharpProjectTypeGuid, 
            VsConstants.VbProjectTypeGuid,
            VsConstants.CppProjectTypeGuid,
            VsConstants.JsProjectTypeGuid,
            VsConstants.CpsProjectTypeGuid,
            VsConstants.SharedProjectTypeGuid,
            VsConstants.FsharpProjectTypeGuid,
            VsConstants.NemerleProjectTypeGuid,
            VsConstants.WixProjectTypeGuid,
            VsConstants.SynergexProjectTypeGuid,
            VsConstants.NomadForVisualStudioProjectTypeGuid,
            VsConstants.TDSProjectTypeGuid,
            VsConstants.DxJsProjectTypeGuid,
            VsConstants.DeploymentProjectTypeGuid
        };

        /// <summary>
        /// Returns the full path of the project directory.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The full path of the project directory.</returns>
        public static string GetFullPath(Project project)
        {
            Debug.Assert(project != null);
            if (project.IsUnloaded())
            {
                // To get the directory of an unloaded project, we use the UniqueName property,
                // which is the path of the project file relative to the solution directory.
                var solutionDirectory = Path.GetDirectoryName(project.DTE.Solution.FullName);
                var projectFileFullPath = Path.Combine(solutionDirectory, project.UniqueName);
                return Path.GetDirectoryName(projectFileFullPath);
            }

            // Attempt to determine the project path using the available EnvDTE.Project properties.
            // Project systems using async load such as CPS may not have all properties populated 
            // for start up scenarios such as VS Templates. In these cases we need to fallback 
            // until we can find one containing the full path.

            // FullPath
            string fullPath = GetPropertyValue<string>(project, "FullPath");

            if (!String.IsNullOrEmpty(fullPath))
            {
                // Some Project System implementations (JS metro app) return the project 
                // file as FullPath. We only need the parent directory
                if (File.Exists(fullPath))
                {
                    return Path.GetDirectoryName(fullPath);
                }

                return fullPath;
            }

            // C++ projects do not have FullPath property, but do have ProjectDirectory one.
            string projectDirectory = GetPropertyValue<string>(project, "ProjectDirectory");

            if (!String.IsNullOrEmpty(projectDirectory))
            {
                return projectDirectory;
            }

            // FullName
            if (!String.IsNullOrEmpty(project.FullName))
            {
                return Path.GetDirectoryName(project.FullName);
            }

            Debug.Fail("Unable to find the project path");

            return null;
        }

        public static bool IsSupported(Project project)
        {
            Debug.Assert(project != null);

            if (project.SupportsINuGetProjectSystem())
            {
                return true;
            }

            return project.Kind != null && _supportedProjectTypes.Contains(project.Kind);
        }

        public static T GetPropertyValue<T>(Project project, string propertyName)
        {
            Debug.Assert(project != null);
            if (project.Properties == null)
            {
                // this happens in unit tests
                return default(T);
            }

            try
            {
                Property property = project.Properties.Item(propertyName);
                if (property != null)
                {
                    // REVIEW: Should this cast or convert?
                    return (T)property.Value;
                }
            }
            catch (ArgumentException)
            {
            }
            return default(T);
        }

        /// <summary>
        /// Gets the EnvDTE.Project instance from IVsHierarchy
        /// </summary>
        /// <param name="pHierarchy">pHierarchy is the IVsHierarchy instance from which the project instance is obtained</param>
        public static Project GetProjectFromHierarchy(IVsHierarchy pHierarchy)
        {
            Debug.Assert(pHierarchy != null);

            // Set it to null to avoid unassigned local variable warning
            Project project = null;
            object projectObject;

            if (pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectObject) >= 0)
            {
                project = (Project)projectObject;
            }

            return project;
        }

        /// <summary>
        /// Gets the path to .nuget folder present in the solution
        /// </summary>
        /// <param name="solution">Solution from which .nuget folder's path is obtained</param>
        public static string GetNuGetSolutionFolder(Solution solution)
        {
            Debug.Assert(solution != null);
            string solutionFilePath = (string)solution.Properties.Item("Path").Value;
            string solutionDirectory = Path.GetDirectoryName(solutionFilePath);
            return Path.Combine(solutionDirectory, NuGetSolutionSettingsFolder);
        }

        /// <summary>
        /// Returns true if the project has the packages.config file
        /// </summary>
        /// <param name="project">Project under whose directory packages.config is searched for</param>
        public static bool PackagesConfigExists(Project project)
        {
            // Here we just check if the packages.config file exists instead of 
            // calling IsNuGetInUse because that will cause NuGet.VisualStudio.dll to get loaded.
            bool isUnloaded = VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
            if (isUnloaded || IsSupported(project))
            {
                Tuple<string, string> configFilePaths = GetPackageReferenceFileFullPaths(project);
                if (File.Exists(configFilePaths.Item1) || File.Exists(configFilePaths.Item2))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the full path of the packages config file associated with the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>A tuple contains full path to packages.project_name.config and packages.config files.</returns>
        public static Tuple<string, string> GetPackageReferenceFileFullPaths(Project project)
        {
            Debug.Assert(project != null);
            var projectDirectory = GetFullPath(project);

            var packagesProjectConfig = Path.Combine(
                projectDirectory ?? String.Empty,
                "packages." + GetName(project) + ".config");

            var packagesConfig = Path.Combine(
                projectDirectory ?? String.Empty,
                PackageReferenceFile);

            return Tuple.Create(packagesProjectConfig, packagesConfig);
        }

        public static void ShowError(ErrorListProvider errorListProvider, TaskErrorCategory errorCategory, TaskPriority priority, string errorText, IVsHierarchy hierarchyItem)
        {
            ErrorTask retargetErrorTask = new ErrorTask();
            retargetErrorTask.Text = errorText;
            retargetErrorTask.ErrorCategory = errorCategory;
            retargetErrorTask.Category = TaskCategory.BuildCompile;
            retargetErrorTask.Priority = priority;
            retargetErrorTask.HierarchyItem = hierarchyItem;
            errorListProvider.Tasks.Add(retargetErrorTask);
            errorListProvider.BringToFront();
            errorListProvider.ForceShowErrors();
        }

        public static Project GetActiveProject(IVsMonitorSelection vsMonitorSelection)
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                vsMonitorSelection.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC);

                if (ppHier == IntPtr.Zero)
                {
                    return null;
                }

                // multiple items are selected.
                if (pitemid == (uint)VSConstants.VSITEMID.Selection)
                {
                    return null;
                }

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null)
                {
                    object project;
                    if (hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project) >= 0)
                    {
                        return project as Project;
                    }
                }

                return null;
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }
        }

        public static bool GetIsSolutionNodeSelected(IVsMonitorSelection vsMonitorSelection)
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                vsMonitorSelection.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC);
                if (pitemid == (uint)VSConstants.VSITEMID.Root)
                {
                    if (ppHier == IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }

            return false;
        }

        public static string GetName(this Project project)
        {
            string name = project.Name;
            if (project.IsJavaScriptProject())
            {
                // The JavaScript project initially returns a "(loading..)" suffix to the project Name.
                // Need to get rid of it for the rest of NuGet to work properly.
                // TODO: Follow up with the VS team to see if this will be fixed eventually
                const string suffix = " (loading...)";
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                }
            }
            return name;
        }

        /// <summary>
        /// This method is different from the GetName() method above in that for Website project, 
        /// it will always return the project name, instead of the full path to the website, when it uses Casini server.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We can treat the website as running IISExpress if we can't get the WebSiteType property.")]
        public static string GetProperName(this Project project)
        {
            if (project.IsWebSite())
            {
                try
                {
                    // if the WebSiteType property of a WebSite project, it means the Website is configured to run with Casini, 
                    // as opposed to IISExpress. In which case, Project.Name will return the full path to the directory of the website. 
                    // We want to extract out the directory name only. 
                    object websiteType = project.Properties.Item("WebSiteType").Value;
                    if (Convert.ToInt32(websiteType, CultureInfo.InvariantCulture) == 0)
                    {
                        // remove the trailing slash. 
                        string projectPath =  project.Name;
                        if (projectPath.Length > 0 && projectPath[projectPath.Length-1] == Path.DirectorySeparatorChar)
                        {
                            projectPath = projectPath.Substring(0, projectPath.Length - 1);
                        }

                        // without the trailing slash, a directory looks like a file name. Hence, call GetFileName gives us the directory name.
                        return Path.GetFileName(projectPath);
                    }
                }
                catch (Exception)
                {
                    // ignore this exception if we can't get the WebSiteType property
                }
            }

            return GetName(project);
        }
    }
}