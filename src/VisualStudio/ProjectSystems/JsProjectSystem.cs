using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This project system represents the JavaScript Metro project in Windows8
    /// </summary>
    public class JsProjectSystem : VsProjectSystem
    {
        public JsProjectSystem(Project project, IFileSystemProvider fileSystemProvider) :
            base(project, fileSystemProvider)
        {
        }

        public override string ProjectName
        {
            get
            {
                return Project.Name;
            }
        }

        public override void AddFile(string path, Stream stream)
        {
            // ensure the parent folder is created before adding file to the project
            Project.GetProjectItems(Path.GetDirectoryName(path), createIfNotExists: true);
            base.AddFile(path, stream);
        }

        protected override void AddFileToContainer(string fullPath, ProjectItems container)
        {
            container.AddFromFile(fullPath);
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            base.DeleteDirectory(path, recursive);

            // In Win8Express beta, there is a bug that causes an empty folder to be removed
            // from project. As a result, VsProjectSystem fails to delete empty folders. 
            // Here, we check if the directory exists on disk and is empty, then we go ahead 
            // and delete it
            if (BaseFileSystem.DirectoryExists(path) &&
                BaseFileSystem.GetFiles(path, "*.*").IsEmpty() &&
                BaseFileSystem.GetDirectories(path).IsEmpty())
            {
                BaseFileSystem.DeleteDirectory(path, recursive: false);
            }
        }
    }
}