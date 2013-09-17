using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This project system represents the JavaScript project in Windows8
    /// </summary>
    public class JsProjectSystem : CpsProjectSystem, IBatchProcessor<string>
    {
        public JsProjectSystem(Project project, IFileSystemProvider fileSystemProvider) :
            base(project, fileSystemProvider)
        {
        }

        public override string ProjectName
        {
            get
            {
                return Project.GetName();
            }
        }

        public override void AddFile(string path, Stream stream)
        {
            // ensure the parent folder is created before adding file to the project            
            Project.GetProjectItems(Path.GetDirectoryName(path), createIfNotExists: true);
            base.AddFile(path, stream);
        }

        public override void AddFile(string path, System.Action<Stream> writeToStream)
        {
            // ensure the parent folder is created before adding file to the project            
            Project.GetProjectItems(Path.GetDirectoryName(path), createIfNotExists: true);
            base.AddFile(path, writeToStream);
        }

        protected override void AddFileToProject(string path)
        {
            if (ExcludeFile(path))
            {
                return;
            }

            string folderPath = Path.GetDirectoryName(path);
            string fullPath = GetFullPath(path);

            // Add the file to project or folder
            ProjectItems container = Project.GetProjectItems(folderPath, createIfNotExists: true);
            if (container == null)
            {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        VsResources.Error_FailedToCreateParentFolder,
                        path,
                        ProjectName));
            }
            AddFileToContainer(fullPath, folderPath, container);

            Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
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

        public void BeginProcessing(IEnumerable<string> batch, PackageAction action)
        {
            // JS projects does not handle TFS operations automatically when calling DTE APIs.
            // We do it manually here. Note the TfsFileSystem implements IBatchProcessor.
            var processor = BaseFileSystem as IBatchProcessor<string>;
            if (processor != null)
            {
                processor.BeginProcessing(batch, action);
            }
        }

        public void EndProcessing()
        {
            var processor = BaseFileSystem as IBatchProcessor<string>;
            if (processor != null)
            {
                processor.EndProcessing();
            }
        }
    }
}