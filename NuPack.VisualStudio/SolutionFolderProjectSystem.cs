namespace NuPack.VisualStudio {
    using System;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;

    public class SolutionFolderProjectSystem : FileBasedProjectSystem {
        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar };
        private readonly Solution _solution;
        private readonly string _folderName;        

        public SolutionFolderProjectSystem(Solution solution, string folderName)
            : base(Path.Combine(Path.GetDirectoryName(solution.FullName), folderName)) {

            _solution = solution;
            _folderName = folderName;
        }

        private Project SolutionFolder {
            get {
                return GetSolutionFolder(_solution, _folderName);
            }
        }

        public override void AddFile(string path, Stream stream) {
            base.AddFile(path, stream);
            string folder = Path.GetDirectoryName(path);
            Project project = GetSolutionFolderProject(folder, createIfNotExists: true);
            project.ProjectItems.AddFromFile(GetFullPath(path));
        }

        public override void DeleteDirectory(string path, bool recursive = false) {            
            var project = GetSolutionFolderProject(path);
            if (project != null) {
                // REVIEW: For some odd reason, the parent project item of nested solution folders
                // look like they are the folder themself and deleting the actual folder directly doesn't update
                // the project items of the parent, while deleting the parent project item does.
                // Figure out why this works.
                if(project.ParentProjectItem != null) {
                    project.ParentProjectItem.Delete();
                }
                else {
                    // If there is no parent then delete the project itself
                    project.Delete();
                }
            }
            
            base.DeleteDirectory(path, recursive);
        }

        public override void DeleteFile(string path) {
            var projectItem = GetSolutionFolderProjectItem(path);
            if (projectItem != null) {
                projectItem.Delete();
            }

            base.DeleteFile(path);
        }

        public override bool DirectoryExists(string path) {
            return base.DirectoryExists(path) || GetSolutionFolderProject(path) != null;
        }

        public override bool FileExists(string path) {
            return base.FileExists(path) || GetSolutionFolderProjectItem(path) != null;
        }

        private Project GetSolutionFolderProject(string path, bool createIfNotExists = false) {
            string[] pathParts = path.Split(PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Aggregate(SolutionFolder, (project, folder) => GetOrCreateSolutionFolder(project, folder, createIfNotExists));
        }

        private ProjectItem GetSolutionFolderProjectItem(string path) {
            string folderPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            Project solutionFolder = GetSolutionFolderProject(folderPath);

            ProjectItem projectItem;
            // If we couldn't get the folder, or the file doesn't exist, return null
            if (solutionFolder == null ||
                !solutionFolder.ProjectItems.TryGetProjectItem(fileName, out projectItem)) {
                return null;
            }

            return projectItem;
        }

        private static Project GetOrCreateSolutionFolder(Project project, string folder, bool createIfNotExists) {
            if (project == null) {
                return null;
            }

            ProjectItem projectItem;
            if (project.ProjectItems.TryGetProjectItem(folder, out projectItem)) {
                return projectItem.Object;
            }
            else if (createIfNotExists) {
                return project.Object.AddSolutionFolder(folder);
            }

            return null;
        }

        private static Project GetSolutionFolder(Solution solution, string solutionFolderName) {
            // Try to get the solution folder if it exists
            Project solutionFolder = (from Project p in solution.Projects
                                      where p.Name.Equals(solutionFolderName, StringComparison.OrdinalIgnoreCase)
                                      select p).FirstOrDefault();

            return solutionFolder ?? ((Solution2)solution).AddSolutionFolder(solutionFolderName);
        }
    }
}