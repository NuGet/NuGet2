using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio.Resources;
using VSLangProj;
using MsBuildProject = Microsoft.Build.Evaluation.Project;
using MsBuildProjectItem = Microsoft.Build.Evaluation.ProjectItem;
using Project = EnvDTE.Project;

namespace NuGet.VisualStudio
{
    public class VsProjectSystem : IProjectSystem, IVsProjectSystem, IComparer<IPackageFile>
    {
        private const string BinDir = "bin";
        private static readonly string[] AssemblyReferencesExtensions = new[] { ".dll", ".exe", ".winmd" };

        private readonly IFileSystem _baseFileSystem;
        private FrameworkName _targetFramework;

        public VsProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
        {
            Project = project;
            _baseFileSystem =  fileSystemProvider.GetFileSystem(project.GetFullPath());
            Debug.Assert(_baseFileSystem != null);
        }

        public ILogger Logger
        {
            get { return _baseFileSystem.Logger; }
            set { _baseFileSystem.Logger = value; }
        }

        public string Root
        {
            get { return _baseFileSystem.Root; }
        }

        protected Project Project
        {
            get;
            private set;
        }

        public virtual string ProjectName
        {
            get
            {
                return Project.Name;
            }
        }

        public string UniqueName
        {
            get
            {
                return Project.UniqueName;
            }
        }

        public FrameworkName TargetFramework
        {
            get
            {
                if (_targetFramework == null)
                {
                    _targetFramework = Project.GetTargetFrameworkName() ?? VersionUtility.DefaultTargetFramework;
                }
                return _targetFramework;
            }
        }

        public virtual void AddFile(string path, Stream stream)
        {
            bool fileExistsInProject = FileExistsInProject(path);

            // If the file exists on disk but not in the project then skip it
            if (_baseFileSystem.FileExists(path) && !fileExistsInProject)
            {
                Logger.Log(MessageLevel.Warning, VsResources.Warning_FileAlreadyExists, path);
            }
            else
            {
                EnsureCheckedOutIfExists(path);
                _baseFileSystem.AddFile(path, stream);
                if (!fileExistsInProject)
                {
                    AddFileToProject(path);
                }
            }
        }

        public void DeleteDirectory(string path, bool recursive = false)
        {
            // Only delete this folder if it is empty and we didn't specify that we want to recurse
            if (!recursive && (_baseFileSystem.GetFiles(path, "*.*").Any() || _baseFileSystem.GetDirectories(path).Any()))
            {
                Logger.Log(MessageLevel.Warning, VsResources.Warning_DirectoryNotEmpty, path);
                return;
            }

            if (Project.DeleteProjectItem(path))
            {
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFolder, path);
            }
        }

        public void DeleteFile(string path)
        {
            if (Project.DeleteProjectItem(path))
            {
                string folderPath = Path.GetDirectoryName(path);
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

        public void AddFrameworkReference(string name)
        {
            try
            {
                // Add a reference to the project
                AddGacReference(name);

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, VsResources.FailedToAddGacReference, name), e);
            }
        }

        protected virtual void AddGacReference(string name)
        {
            Project.Object.References.Add(name);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public virtual void AddReference(string referencePath, Stream stream)
        {
            string name = Path.GetFileNameWithoutExtension(referencePath);

            try
            {
                // Get the full path to the reference
                string fullPath = PathUtility.GetAbsolutePath(Root, referencePath);

                string assemblyPath = fullPath;
                bool usedTempFile = false;

                // There is a bug in Visual Studio whereby if the fullPath contains a comma, 
                // then calling Project.Object.References.Add() on it will throw a COM exception.
                // To work around it, we copy the assembly into temp folder and add reference to the copied assembly
                if (fullPath.Contains(","))
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(fullPath));
                    File.Copy(fullPath, tempFile, true);
                    assemblyPath = tempFile;
                    usedTempFile = true;
                }

                // Add a reference to the project
                Reference reference = Project.Object.References.Add(assemblyPath);

                // if we copied the assembly to temp folder earlier, delete it now since we no longer need it.
                if (usedTempFile)
                {
                    try
                    {
                        File.Delete(assemblyPath);
                    }
                    catch
                    {
                        // don't care if we fail to delete a temp file
                    }
                }

                // Always set copy local to true for references that we add
                try
                {
                  reference.CopyLocal = true;
                }
                catch (NotSupportedException)
                {

                }
                catch (NotImplementedException)
                {

                }


                // This happens if the assembly appears in any of the search paths that VS uses to locate assembly references.
                // Most commonly, it happens if this assembly is in the GAC or in the output path.
                if (!reference.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Get the msbuild project for this project
                    MsBuildProject buildProject = Project.AsMSBuildProject();

                    if (buildProject != null)
                    {
                        // Get the assembly name of the reference we are trying to add
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(fullPath);

                        // Try to find the item for the assembly name
                        MsBuildProjectItem item = (from assemblyReferenceNode in buildProject.GetAssemblyReferences()
                                                   where AssemblyNamesMatch(assemblyName, assemblyReferenceNode.Item2)
                                                   select assemblyReferenceNode.Item1).FirstOrDefault();

                        if (item != null)
                        {
                            // Add the <HintPath> metadata item as a relative path
                            item.SetMetadataValue("HintPath", referencePath);

                            // Save the project after we've modified it.
                            Project.Save();
                        }
                    }
                }

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, VsResources.FailedToAddReference, name), e);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public virtual void RemoveReference(string name)
        {
            try
            {
                // Get the reference name without extension
                string referenceName = Path.GetFileNameWithoutExtension(name);

                // Remove the reference from the project
                var reference = Project.Object.References.Item(referenceName);
                if (reference != null)
                {
                    reference.Remove();
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemoveReference, name, ProjectName);
                }
            }
            catch (Exception e)
            {
                Logger.Log(MessageLevel.Warning, e.Message);
            }
        }

        public bool FileExists(string path)
        {
            // Only check the project system if the file is on disk to begin with
            return _baseFileSystem.FileExists(path) || FileExistsInProject(path);
        }

        private bool FileExistsInProject(string path)
        {
            return Project.GetProjectItem(path) != null;
        }

        protected virtual bool ExcludeFile(string path)
        {
            // Exclude files from the bin directory.
            return BinDir.Equals(Path.GetDirectoryName(path), StringComparison.OrdinalIgnoreCase);
        }

        protected virtual void AddFileToProject(string path)
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
                ProjectItems container = Project.GetProjectItems(folderPath, createIfNotExists: true);
                // Add the file to project or folder
                AddFileToContainer(fullPath, container);
            });


            Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
        }

        protected virtual void AddFileToContainer(string fullPath, ProjectItems container)
        {
            container.AddFromFileCopy(fullPath);
        }

        public virtual string ResolvePath(string path)
        {
            return path;
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            // Get all physical files
            return from p in Project.GetChildItems(path, filter, VsConstants.VsProjectItemKindPhysicalFile)
                   select p.Name;
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            // Get all physical folders
            return from p in Project.GetChildItems(path, "*.*", VsConstants.VsProjectItemKindPhysicalFolder)
                   select p.Name;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We never want to fail when checking for existance")]
        public virtual bool ReferenceExists(string name)
        {
            try
            {
                string referenceName = name;

                if (AssemblyReferencesExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                {
                    // Get the reference name without extension
                    referenceName = Path.GetFileNameWithoutExtension(name);
                }

                return Project.Object.References.Item(referenceName) != null;
            }
            catch
            {
            }
            return false;
        }

        public virtual dynamic GetPropertyValue(string propertyName)
        {
            try
            {
                Property property = Project.Properties.Item(propertyName);
                if (property != null)
                {
                    return property.Value;
                }
            }
            catch (ArgumentException)
            {
                // If the property doesn't exist this will throw an argument exception
            }
            return null;
        }

        public virtual bool IsSupportedFile(string path)
        {
            return !("web.config".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureCheckedOutIfExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (FileExists(path) &&
                Project.DTE.SourceControl != null &&
                Project.DTE.SourceControl.IsItemUnderSCC(fullPath) &&
                !Project.DTE.SourceControl.IsItemCheckedOut(fullPath))
            {

                // Check out the item
                Project.DTE.SourceControl.CheckOutItem(fullPath);
            }
        }

        private static bool AssemblyNamesMatch(AssemblyName name1, AssemblyName name2)
        {
            return name1.Name.Equals(name2.Name, StringComparison.OrdinalIgnoreCase) &&
                   EqualsIfNotNull(name1.Version, name2.Version) &&
                   EqualsIfNotNull(name1.CultureInfo, name2.CultureInfo) &&
                   EqualsIfNotNull(name1.GetPublicKeyToken(), name2.GetPublicKeyToken(), Enumerable.SequenceEqual);
        }

        private static bool EqualsIfNotNull<T>(T obj1, T obj2)
        {
            return EqualsIfNotNull(obj1, obj2, (a, b) => a.Equals(b));
        }

        private static bool EqualsIfNotNull<T>(T obj1, T obj2, Func<T, T, bool> equals)
        {
            // If both objects are non null do the equals
            if (obj1 != null && obj2 != null)
            {
                return equals(obj1, obj2);
            }

            // Otherwise consider them equal if either of the values are null
            return true;
        }

        public string GetFullPath(string path)
        {
            return _baseFileSystem.GetFullPath(path);
        }

        public virtual bool DirectoryExists(string path)
        {
            return _baseFileSystem.DirectoryExists(path);
        }

        public Stream OpenFile(string path)
        {
            return _baseFileSystem.OpenFile(path);
        }

        public DateTimeOffset GetLastModified(string path)
        {
            return _baseFileSystem.GetLastModified(path);
        }

        public DateTimeOffset GetCreated(string path)
        {
            return _baseFileSystem.GetCreated(path);
        }

        public int Compare(IPackageFile x, IPackageFile y)
        {
            // BUG 636: We sort files so that they are added in the correct order
            // e.g aspx before aspx.cs

            if (x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            // Add files that are prefixes of other files first
            if (x.Path.StartsWith(y.Path, StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            if (y.Path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return y.Path.CompareTo(x.Path);
        }
    }
}