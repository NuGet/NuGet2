using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    public class VsProjectSystem : PhysicalFileSystem, IProjectSystem {
        private const string BinDir = "bin";

        private FrameworkName _targetFramework;

        public VsProjectSystem(Project project)
            : base(project.GetFullPath()) {
            Project = project;
        }

        protected Project Project {
            get;
            private set;
        }

        public virtual string ProjectName {
            get {
                return Project.Name;
            }
        }

        public FrameworkName TargetFramework {
            get {
                if (_targetFramework == null) {
                    _targetFramework = GetTargetFramework() ?? VersionUtility.DefaultTargetFramework;
                }
                return _targetFramework;
            }
        }

        private FrameworkName GetTargetFramework() {
            string targetFrameworkMoniker = Project.GetPropertyValue<string>("TargetFrameworkMoniker");
            if (targetFrameworkMoniker != null) {
                return new FrameworkName(targetFrameworkMoniker);
            }

            return null;
        }

        public override void AddFile(string path, Stream stream) {
            EnsureCheckedOutIfExists(path);

            base.AddFile(path, stream);
            AddFileToProject(path);
        }

        public override void DeleteDirectory(string path, bool recursive = false) {
            if (Project.DeleteProjectItem(path)) {
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFolder, path);
            }
        }

        public override void DeleteFile(string path) {
            if (Project.DeleteProjectItem(path)) {
                string folderPath = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(folderPath)) {
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
                }
                else {
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFile, Path.GetFileName(path));
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public virtual void AddReference(string referencePath, Stream stream) {
            try {
                string name = Path.GetFileNameWithoutExtension(referencePath);

                // Add a reference to the project
                Project.Object.References.Add(GetAbsolutePath(referencePath));

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch {

            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public virtual void RemoveReference(string name) {
            try {
                // Get the reference name without extension
                string referenceName = Path.GetFileNameWithoutExtension(name);

                // Remove the reference from the project
                var reference = Project.Object.References.Item(referenceName);
                if (reference != null) {
                    reference.Remove();
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemoveReference, name, ProjectName);
                }
            }
            catch {
            }
        }

        public override bool FileExists(string path) {
            // See if the file is in the project system
            return Project.GetProjectItem(path) != null;
        }

        protected virtual bool ExcludeFile(string path) {
            // Exclude files from the bin directory.
            return Path.GetDirectoryName(path).Equals(BinDir, StringComparison.OrdinalIgnoreCase);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        protected virtual void AddFileToProject(string path) {
            if (ExcludeFile(path)) {
                return;
            }

            // Get the project items for the folder path
            string folderPath = Path.GetDirectoryName(path);
            ProjectItems container = Project.GetProjectItems(folderPath, createIfNotExists: true);

            try {
                // Add the file to the project
                string fullPath = GetFullPath(path);
                container.AddFromFileCopy(fullPath);

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddedFileToProject, path, ProjectName);
            }
            catch {

            }
        }

        public override IEnumerable<string> GetFiles(string path, string filter) {
            // Get all physical files
            return from p in Project.GetChildItems(path, filter, VsConstants.VsProjectItemKindPhysicalFile)
                   select p.Name;
        }

        public override IEnumerable<string> GetDirectories(string path) {
            // Get all physical folders
            return from p in Project.GetChildItems(path, "*.*", VsConstants.VsProjectItemKindPhysicalFolder)
                   select p.Name;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We never want to fail when checking for existance")]
        public virtual bool ReferenceExists(string name) {
            try {
                // Get the reference name without extension
                string referenceName = Path.GetFileNameWithoutExtension(name);

                return Project.Object.References.Item(referenceName) != null;
            }
            catch {
            }
            return false;
        }

        public virtual dynamic GetPropertyValue(string propertyName) {
            try {
                Property property = Project.Properties.Item(propertyName);
                if (property != null) {
                    return property.Value;
                }
            }
            catch(ArgumentException) {
                // If the property doesn't exist this will throw an argument exception
            }
            return null;
        }

        public virtual bool IsSupportedFile(string path) {
            return !(Path.GetFileName(path).Equals("web.config", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes a path relative to the root to an absolute path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string GetAbsolutePath(string path) {
            return Path.GetFullPath(Path.Combine(Root, path));
        }

        private void EnsureCheckedOutIfExists(string path) {
            string fullPath = GetFullPath(path);
            if (FileExists(path) &&
                Project.DTE.SourceControl != null &&
                Project.DTE.SourceControl.IsItemUnderSCC(fullPath) &&
                !Project.DTE.SourceControl.IsItemCheckedOut(fullPath)) {

                // Check out the item
                Project.DTE.SourceControl.CheckOutItem(fullPath);
            }
        }
    }
}
