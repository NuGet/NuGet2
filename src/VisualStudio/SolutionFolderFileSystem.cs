using EnvDTE;
using System;
using System.IO;

namespace NuGet.VisualStudio
{
    // TODO: Redo this to share code with VsProjectSystem.
    public class SolutionFolderFileSystem : PhysicalFileSystem
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
            AddFileCore(path, () => base.AddFile(path, stream));
        }

        public override void AddFile(string path, Action<Stream> writeToStream)
        {
            AddFileCore(path, () => base.AddFile(path, writeToStream));
        }

        private void AddFileCore(string path, Action action)
        {
            EnsureSolutionFolder();
            if (_solutionFolder != null)
            {
                bool fileExistsInProject = FileExistsInProject(path);
                if (fileExistsInProject)
                {
                    // If the file already exists, check it out.
                    _solutionFolder.EnsureCheckedOutIfExists(this, path);
                }

                action();

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
    }
}
