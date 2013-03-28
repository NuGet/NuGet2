using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    public class NativeProjectSystem : VsProjectSystem
    {
        public NativeProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        public override bool IsBindingRedirectSupported
        {
            get
            {
                return false;
            }
        }

        protected override void AddGacReference(string name)
        {
            // Native project doesn't know about GAC
        }

        public override bool ReferenceExists(string name)
        {
            // We disable assembly reference for native projects
            return true;
        }

        public override void AddReference(string referencePath, Stream stream)
        {
            // We disable assembly reference for native projects
        }

        public override void RemoveReference(string name)
        {
            // We disable assembly reference for native projects
        }

        public override void AddImport(string targetPath, ProjectImportLocation location)
        {
            if (VsVersionHelper.IsVisualStudio2010)
            {
                base.AddImport(targetPath, location);
            }
            else
            {
                // For VS 2012 or above, the operation has to be done inside the Writer lock

                if (String.IsNullOrEmpty(targetPath))
                {
                    throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "targetPath");
                }

                string relativeTargetPath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetPath);
                Project.DoWorkInWriterLock(buildProject => buildProject.AddImportStatement(relativeTargetPath, location));
                Project.Save();
            }
        }

        public override void RemoveImport(string targetPath)
        {
            if (VsVersionHelper.IsVisualStudio2010)
            {
                base.RemoveImport(targetPath);
            }
            else
            {
                // For VS 2012 or above, the operation has to be done inside the Writer lock

                if (String.IsNullOrEmpty(targetPath))
                {
                    throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "targetPath");
                }
                string relativeTargetPath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetPath);
                Project.DoWorkInWriterLock(buildProject => buildProject.RemoveImportStatement(relativeTargetPath));
                Project.Save();
            }
        }

        protected override void AddFileToContainer(string fullPath, ProjectItems container)
        {
            container.AddFromFile(Path.GetFileName(fullPath));
        }

        protected override void AddFileToProject(string path)
        {
            if (ExcludeFile(path))
            {
                return;
            }

            // Get the project items for the folder path
            string folderPath = Path.GetDirectoryName(path);
            string fullPath = GetFullPath(path);

            ThreadHelper.Generic.Invoke(() =>
            {
                ProjectItems container = GetFolder(folderPath);

                // Add the file to project or folder
                AddFileToContainer(fullPath, container);
            });

            Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
        }

        private ProjectItems GetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return Project.ProjectItems;
            }

            ProjectItem item = Project.ProjectItems.Item(folderPath);            
            if (item == null)
            {
                var vcProject = Project.Object as VCProject;
                vcProject.AddFilter(folderPath);
                item = Project.ProjectItems.Item(folderPath);
            }
            
            return item.ProjectItems;
        }
    }
}
