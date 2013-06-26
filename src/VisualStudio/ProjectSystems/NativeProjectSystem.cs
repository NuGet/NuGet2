using System;
using System.IO;
using System.Runtime.CompilerServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
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
                if (VsVersionHelper.IsVisualStudio2012)
                {
                    Project.DoWorkInWriterLock(buildProject => buildProject.AddImportStatement(relativeTargetPath, location));
                    Project.Save();
                }
                else
                {
                    AddImportStatementForVS2013(location, relativeTargetPath);
                }
            }
        }

        // IMPORTANT: The NoInlining is required to prevent CLR from loading VisualStudio12.dll assembly while running 
        // in VS2010 and VS2012
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddImportStatementForVS2013(ProjectImportLocation location, string relativeTargetPath)
        {
            NuGet.VisualStudio12.ProjectHelper.DoWorkInWriterLock(
                Project,
                Project.ToVsHierarchy(),
                buildProject => buildProject.AddImportStatement(relativeTargetPath, location));
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

                if (VsVersionHelper.IsVisualStudio2012)
                {
                    Project.DoWorkInWriterLock(buildProject => buildProject.RemoveImportStatement(relativeTargetPath));
                    Project.Save();
                }
                else
                {
                    RemoveImportStatementForVS2013(relativeTargetPath);
                }
            }
        }

        // IMPORTANT: The NoInlining is required to prevent CLR from loading VisualStudio12.dll assembly while running 
        // in VS2010 and VS2012
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RemoveImportStatementForVS2013(string relativeTargetPath)
        {
            NuGet.VisualStudio12.ProjectHelper.DoWorkInWriterLock(
                Project,
                Project.ToVsHierarchy(),
                buildProject => buildProject.RemoveImportStatement(relativeTargetPath));
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
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    AddFileToProjectForVS2010(folderPath, fullPath);
                }
                else
                {
                    VCProjectHelper.AddFileToProject(Project.Object, fullPath, folderPath);
                }
            });

            Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
        }

        public override void DeleteFile(string path)
        {
            string folderPath = Path.GetDirectoryName(path);
            string fullPath = GetFullPath(path);

            bool succeeded;
            if (VsVersionHelper.IsVisualStudio2010)
            {
                succeeded = RemoveFileFromProjectForVS2010(folderPath, fullPath);
            }
            else 
            {
                succeeded = VCProjectHelper.RemoveFileFromProject(Project.Object, fullPath, folderPath);
            }
            
            if (succeeded)
            {
                // The RemoveFileFromProject() method only removes file from project.
                // We want to delete it from disk too.
                BaseFileSystem.DeleteFileAndParentDirectoriesIfEmpty(path);

                if (!String.IsNullOrEmpty(folderPath))
                {
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
                }
                else
                {
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFile, Path.GetFileName(path));
                }
            }
        }

        // Use NoInlining option to prevent the CLR from loading VisualStudio10.dll when running inside VS 2013
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddFileToProjectForVS2010(string folderPath, string fullPath)
        {
            VisualStudio10.VCProjectHelper.AddFileToProject(Project.Object, fullPath, folderPath);
        }

        // Use NoInlining option to prevent the CLR from loading VisualStudio10.dll when running inside VS 2013
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool RemoveFileFromProjectForVS2010(string folderPath, string fullPath)
        {
            return VisualStudio10.VCProjectHelper.RemoveFileFromProject(Project.Object, fullPath, folderPath);
        }
    }
}