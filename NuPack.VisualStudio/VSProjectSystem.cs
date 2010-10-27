namespace NuGet.VisualStudio {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using EnvDTE;
    using NuGet.VisualStudio.Resources;

    public class VsProjectSystem : FileBasedProjectSystem {
        private const string BinDir = "bin";

        private FrameworkName _targetFramework;

        public VsProjectSystem(Project project)
            : base(project.GetPropertyValue<string>("FullPath")) {
            Project = project;
        }

        protected Project Project {
            get;
            private set;
        }

        public override string ProjectName {
            get {
                return Project.Name;
            }
        }

        public override FrameworkName TargetFramework {
            get {
                if (_targetFramework == null) {
                    _targetFramework = GetTargetFramework() ?? base.TargetFramework;
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
        public override void AddReference(string referencePath) {
            try {
                string name = Path.GetFileNameWithoutExtension(referencePath);

                // Add a reference to the project
                Project.Object.References.Add(referencePath);

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch {

            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public override void RemoveReference(string name) {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override bool ReferenceExists(string name) {
            try {
                // Get the reference name without extension
                string referenceName = Path.GetFileNameWithoutExtension(name);

                return Project.Object.References.Item(referenceName) != null;
            }
            catch {
            }
            return false;
        }

        public override dynamic GetPropertyValue(string propertyName) {
            Property property = Project.Properties.Item(propertyName);
            if (property != null) {
                return property.Value;
            }
            return null;
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
