using EnvDTE;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using MsBuildProject = Microsoft.Build.Evaluation.Project;
using MsBuildProjectItem = Microsoft.Build.Evaluation.ProjectItem;
using Project = EnvDTE.Project;

namespace NuGet.VisualStudio
{
    public class VsProjectSystem : PhysicalFileSystem, IVsProjectSystem, IComparer<IPackageFile>
    {
        private const string BinDir = "bin";

        private FrameworkName _targetFramework;
        private readonly IFileSystem _baseFileSystem;

        public VsProjectSystem(Project project, IFileSystemProvider fileSystemProvider) 
            : base(project.GetFullPath())
        {
            Project = project;
            _baseFileSystem = fileSystemProvider.GetFileSystem(project.GetFullPath());
            Debug.Assert(_baseFileSystem != null);
        }

        protected Project Project
        {
            get;
            private set;
        }

        protected IFileSystem BaseFileSystem
        {
            get
            {
                return _baseFileSystem;
            }
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
                return Project.GetUniqueName();
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

        public virtual bool IsBindingRedirectSupported
        {
            get
            {
                // Silverlight projects and Windows Phone projects do not support binding redirect. 
                // They both share the same identifier as "Silverlight"
                return !"Silverlight".Equals(TargetFramework.Identifier, StringComparison.OrdinalIgnoreCase);
            }
        }

        public override void AddFile(string path, Stream stream)
        {
            AddFileCore(path, () => base.AddFile(path, stream));
        }

        public override void AddFile(string path, Action<Stream> writeToStream)
        {
            AddFileCore(path, () => base.AddFile(path, writeToStream));
        }

        private void AddFileCore(string path, Action addFile)
        {
            bool fileExistsInProject = FileExistsInProject(path);

            // If the file exists on disk but not in the project then skip it.
            // One exception is the 'packages.config' file, in which case we want to include
            // it into the project.
            if (base.FileExists(path) && !fileExistsInProject && !path.Equals(Constants.PackageReferenceFile))
            {
                Logger.Log(MessageLevel.Warning, VsResources.Warning_FileAlreadyExists, path);
            }
            else
            {
                EnsureCheckedOutIfExists(path);
                addFile();
                if (!fileExistsInProject)
                {
                    AddFileToProject(path);
                }
            }
        }

        public override Stream CreateFile(string path)
        {
            EnsureCheckedOutIfExists(path);
            return base.CreateFile(path);
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            // Only delete this folder if it is empty and we didn't specify that we want to recurse
            if (!recursive && (base.GetFiles(path, "*.*", recursive).Any() || base.GetDirectories(path).Any()))
            {
                Logger.Log(MessageLevel.Warning, VsResources.Warning_DirectoryNotEmpty, path);
                return;
            }

            // Workaround for TFS update issue. If we're bound to TFS, do not try and delete directories.
            if (!(_baseFileSystem is ISourceControlFileSystem) && Project.DeleteProjectItem(path))
            {
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFolder, path);
            }
        }

        public override void DeleteFile(string path)
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
            Project.GetReferences().Add(name);
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
                dynamic reference = Project.GetReferences().Add(assemblyPath);

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

                if (reference != null)
                {   
                    // This happens if the assembly appears in any of the search paths that VS uses to locate assembly references.
                    // Most commonly, it happens if this assembly is in the GAC or in the output path.
                    if (reference.Path != null && !reference.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
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

                                // Set <Private> to true
                                item.SetMetadataValue("Private", "True");

                                // Save the project after we've modified it.
                                Project.Save(this);
                            }
                        }
                    }
                    else
                    {
                        TrySetSpecificVersion(reference);
                        TrySetCopyLocal(reference);
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
                // NOTE:- Project.Object.References.Item requires Reference.Identity
                //        which is, the Assembly name without path or extension
                //        But, we pass in the assembly file name. And, this works for
                //        almost all the assemblies since Assembly Name is the same as the assembly file name
                //        In case of F#, the input parameter is case-sensitive as well
                //        Hence, an override to THIS function is added to take care of that
                var reference = Project.GetReferences().Item(referenceName);
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

        public virtual bool FileExistsInProject(string path)
        {
            return Project.ContainsFile(path);
        }

        protected virtual bool ExcludeFile(string path)
        {
            // Exclude files from the bin directory.
            return Path.GetDirectoryName(path).Equals(BinDir, StringComparison.OrdinalIgnoreCase);
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
                AddFileToContainer(fullPath, folderPath, container);
            });

            Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
        }

        protected virtual void AddFileToContainer(string fullPath, string folderPath, ProjectItems container)
        {
            container.AddFromFileCopy(fullPath);
        }

        public virtual string ResolvePath(string path)
        {
            return path;
        }

        public override IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            if (recursive)
            {
                throw new NotSupportedException();
            }
            else
            {
                // Get all physical files
                return from p in Project.GetChildItems(path, filter, VsConstants.VsProjectItemKindPhysicalFile)
                       select p.Name;
            }
        }

        public override IEnumerable<string> GetDirectories(string path)
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

                if (Constants.AssemblyReferencesExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                {
                    // Get the reference name without extension
                    referenceName = Path.GetFileNameWithoutExtension(name);
                }

                return Project.GetReferences().Item(referenceName) != null;
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

        public virtual void AddImport(string targetPath, ProjectImportLocation location)
        {
            if (String.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "targetPath");
            }

            string relativeTargetPath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetPath);
            Project.AddImportStatement(relativeTargetPath, location);

            Project.Save(this);

            // notify the project system of the change
            UpdateImportStamp(Project);
        }

        public virtual void RemoveImport(string targetPath)
        {
            if (String.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "targetPath");
            }
            string relativeTargetPath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetPath);
            Project.RemoveImportStatement(relativeTargetPath);
            Project.Save(this);

            // notify the project system of the change
            UpdateImportStamp(Project);
        }

        public virtual bool IsSupportedFile(string path)
        {
            string fileName = Path.GetFileName(path);

            // exclude all file names with the pattern as "web.*.config", 
            // e.g. web.config, web.release.config, web.debug.config
            return !(fileName.StartsWith("web.", StringComparison.OrdinalIgnoreCase) &&
                     fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureCheckedOutIfExists(string path)
        {
            Project.EnsureCheckedOutIfExists(this, path);        
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

        /// <summary>
        /// Sets NuGetPackageImportStamp to a new random guid. This is a hack to let the project system know it is out of date.
        /// The value does not matter, it just needs to change.
        /// </summary>
        protected static void UpdateImportStamp(Project project)
        {
            // There is no reason to call this for pre-Dev12 project systems.
            if (VsVersionHelper.IsVisualStudio2013)
            {
                IVsBuildPropertyStorage propStore = project.ToVsHierarchy() as IVsBuildPropertyStorage;
                if (propStore != null)
                {
                    // <NuGetPackageImportStamp>af617720</NuGetPackageImportStamp>
                    string stamp = Guid.NewGuid().ToString().Split('-')[0];
                    ErrorHandler.ThrowOnFailure(propStore.SetPropertyValue("NuGetPackageImportStamp", string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, stamp));
                }
            }
        }

        private static void TrySetCopyLocal(dynamic reference)
        {
            // Always set copy local to true for references that we add
            try
            {
                // In order to properly write this to MSBuild in ALL cases, we have to trigger the Property Change
                // notification with a new value of "true". However, "true" is the default value, so in order to
                // cause a notification to fire, we have to set it to false and then back to true
                reference.CopyLocal = false;
                reference.CopyLocal = true;
            }
            catch (NotSupportedException)
            {

            }
            catch (NotImplementedException)
            {

            }
        }

        // Set SpecificVersion to true
        private static void TrySetSpecificVersion(dynamic reference)
        {
            // Always set SpecificVersion to true for references that we add
            try
            {
                reference.SpecificVersion = false;
                reference.SpecificVersion = true;
            }
            catch (NotSupportedException)
            {

            }
            catch (NotImplementedException)
            {

            }
            // 'Microsoft.VisualStudio.FSharp.ProjectSystem.Automation.OAAssemblyReference' does not contain
            // a definition for 'SpecificVersion'.
            catch (RuntimeBinderException)
            {

            }
        }
    }
}