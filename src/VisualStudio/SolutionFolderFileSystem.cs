using EnvDTE;
using System.IO;

namespace NuGet.VisualStudio
{
    // TODO: Redo this to share code with VsProjectSystem.
    internal class SolutionFolderFileSystem : PhysicalFileSystem
    {
        private readonly Solution _solution;
        private readonly string _solutionFolderPath;
        private bool _ensureProjectCalled;
        private Project _solutionFolder;

        public SolutionFolderFileSystem(Solution solution, string solutionFolderPath, string physicalPath) 
            : base(physicalPath)
        {
            _solution = solution;
            _solutionFolderPath = solutionFolderPath;
        }

        public override void AddFile(string path, Stream stream)
        {
            EnsureSolutionFolder();
            if (_solutionFolder != null)
            {
                bool fileExistsInProject = FileExistsInProject(path);
                if (fileExistsInProject)
                {
                    // If the file already exists, check it out.
                    EnsureCheckedOutIfExists(path);
                }
                base.AddFile(path, stream);
                
                if (!fileExistsInProject)
                {
                    // Add the file to the solution directory if it does not already exist.
                    var fullPath = GetFullPath(path);
                    _solutionFolder.ProjectItems.AddFromFile(fullPath);
                }
            }
        }

        public override void DeleteFile(string path)
        {
            EnsureSolutionFolder();
            if (_solutionFolder != null && FileExistsInProject(path))
            {
                _solutionFolder.DeleteProjectItem(path);
            }
            base.DeleteFile(path);
        }

        private void EnsureSolutionFolder()
        {
            if (!_ensureProjectCalled)
            {
                _ensureProjectCalled = true;
                _solutionFolder = _solution.GetSolutionFolder(_solutionFolderPath);
            }
        }

        private bool FileExistsInProject(string path)
        {
            return _solutionFolder.GetProjectItem(path) != null;
        }

        private void EnsureCheckedOutIfExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (FileExists(path) &&
                _solutionFolder.DTE.SourceControl != null &&
                _solutionFolder.DTE.SourceControl.IsItemUnderSCC(fullPath) &&
                !_solutionFolder.DTE.SourceControl.IsItemCheckedOut(fullPath))
            {

                // Check out the item
                _solutionFolder.DTE.SourceControl.CheckOutItem(fullPath);
            }
        }
    }
}
