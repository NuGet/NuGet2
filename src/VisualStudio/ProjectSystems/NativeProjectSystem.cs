using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    public class NativeProjectSystem : CpsProjectSystem
    {
        public NativeProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
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

        public override IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            var allFiles = ThreadHelper.Generic.Invoke<IEnumerable<string>>(() =>
            {
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    return GetFilesFromProjectForVS2010(path);
                }
                else
                {
                    return VCProjectHelper.GetFiles(Project.Object, path);
                }
            });

            if (filter == null || filter.Equals("*.*", StringComparison.OrdinalIgnoreCase)) 
            {
                return allFiles;
            }

            Regex matcher = ProjectExtensions.GetFilterRegex(filter);
            return allFiles.Where(f => matcher.IsMatch(f));
        }

        public override IEnumerable<string> GetDirectories(string path)
        {
            return ThreadHelper.Generic.Invoke<IEnumerable<string>>(() =>
            {
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    return GetFiltersFromProjectForVS2010(path);
                }
                else
                {
                    return VCProjectHelper.GetFilters(Project.Object, path);
                }
            });
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

        // Use NoInlining option to prevent the CLR from loading VisualStudio10.dll when running inside VS 2013
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] GetFilesFromProjectForVS2010(string folderPath)
        {
            return VisualStudio10.VCProjectHelper.GetFiles(Project.Object, folderPath).ToArray();
        }

        // Use NoInlining option to prevent the CLR from loading VisualStudio10.dll when running inside VS 2013
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] GetFiltersFromProjectForVS2010(string folderPath)
        {
            return VisualStudio10.VCProjectHelper.GetFilters(Project.Object, folderPath).ToArray();
        }
    }
}