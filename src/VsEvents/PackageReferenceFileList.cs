using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.VsEvents
{
    /// <summary>
    /// This class contains the list of all package reference files used by a solution and all projects
    /// contained in the solution.
    /// </summary>
    internal class PackageReferenceFileList
    {
        // the full path of the package reference file in the nuget solution folder ($solutionDir\.nuget).
        private readonly string _solutionPackageReferenceFile;

        // the collection of package reference files used by projects in the solution.
        private readonly List<ProjectPackageReferenceFile> _projectPackageReferenceFiles;

        public PackageReferenceFileList(Solution solution)
        {
            _projectPackageReferenceFiles = new List<ProjectPackageReferenceFile>();

            var packageReferenceFileName = Path.Combine(
                VsUtility.GetNuGetSolutionFolder(solution),
                VsUtility.PackageReferenceFile);
            if (File.Exists(packageReferenceFileName))
            {
                _solutionPackageReferenceFile = packageReferenceFileName;
            }

            foreach (Project project in solution.Projects)
            {
                GetPackageReferenceFiles(project);
            }
        }

        public string SolutionPackageReferenceFile
        {
            get { return _solutionPackageReferenceFile; }
        }

        public ICollection<ProjectPackageReferenceFile> ProjectPackageReferenceFiles
        {
            get { return _projectPackageReferenceFiles; }
        }

        /// <summary>
        /// Indicates whether the solution itself and projects contained in the solution use any 
        /// package reference files. True if no package reference files are used.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _solutionPackageReferenceFile == null && _projectPackageReferenceFiles.Count == 0;
            }
        }

        /// <summary>
        /// Gets package reference file(s) used by the project, or subprojects if the project is a solution folder,
        /// and adds the files into list _projectPackageReferenceFiles.
        /// </summary>
        /// <param name="project">the project to inspect.</param>
        private void GetPackageReferenceFiles(Project project)
        {
            if (project == null)
            {
                return;
            }

            // Ignore "Miscellaneous Files" project
            if (VsConstants.VsProjectKindMisc.Equals(project.Kind, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (VsConstants.VsProjectItemKindSolutionFolder.Equals(project.Kind, StringComparison.OrdinalIgnoreCase))
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    var nestedProject = item.SubProject;
                    GetPackageReferenceFiles(nestedProject);
                }
            }
            else
            {
                Tuple<string, string> packageReferenceFiles = VsUtility.GetPackageReferenceFileFullPaths(project);
                if (File.Exists(packageReferenceFiles.Item1))
                {
                    _projectPackageReferenceFiles.Add(new ProjectPackageReferenceFile(project, packageReferenceFiles.Item1));
                }
                else if (File.Exists(packageReferenceFiles.Item2))
                {
                    _projectPackageReferenceFiles.Add(new ProjectPackageReferenceFile(project, packageReferenceFiles.Item2));
                }
            }
        }
    }
}
