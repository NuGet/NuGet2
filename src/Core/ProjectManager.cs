namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Xml.Linq;
    using Microsoft.Internal.Web.Utils;
    using NuGet.Resources;

    public class ProjectManager : IProjectManager {
        private event EventHandler<PackageOperationEventArgs> _packageReferenceAdding;
        private event EventHandler<PackageOperationEventArgs> _packageReferenceAdded;
        private event EventHandler<PackageOperationEventArgs> _packageReferenceRemoving;
        private event EventHandler<PackageOperationEventArgs> _packageReferenceRemoved;

        private ILogger _logger;

        // REVIEW: These should be externally pluggable
        private static readonly IDictionary<string, IPackageFileTransformer> _fileTransformers = new Dictionary<string, IPackageFileTransformer>(StringComparer.OrdinalIgnoreCase) {
            { ".transform", new XmlTransfomer(GetConfigMappings()) },
            { ".pp", new Preprocessor() }
        };

        public ProjectManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IProjectSystem project, IPackageRepository localRepository) {
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (pathResolver == null) {
                throw new ArgumentNullException("pathResolver");
            }
            if (project == null) {
                throw new ArgumentNullException("project");
            }
            if (localRepository == null) {
                throw new ArgumentNullException("localRepository");
            }

            SourceRepository = sourceRepository;
            Project = project;
            PathResolver = pathResolver;
            LocalRepository = localRepository;
        }

        public IPackagePathResolver PathResolver {
            get;
            private set;
        }

        public IPackageRepository LocalRepository {
            get;
            private set;
        }

        public IPackageRepository SourceRepository {
            get;
            private set;
        }

        public IProjectSystem Project {
            get;
            private set;
        }

        public ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding {
            add {
                _packageReferenceAdding += value;
            }
            remove {
                _packageReferenceAdding -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded {
            add {
                _packageReferenceAdded += value;
            }
            remove {
                _packageReferenceAdded -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving {
            add {
                _packageReferenceRemoving += value;
            }
            remove {
                _packageReferenceRemoving -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved {
            add {
                _packageReferenceRemoved += value;
            }
            remove {
                _packageReferenceRemoved -= value;
            }
        }

        public virtual void AddPackageReference(string packageId) {
            AddPackageReference(packageId, version: null, ignoreDependencies: false);
        }

        public virtual void AddPackageReference(string packageId, Version version) {
            AddPackageReference(packageId, version: version, ignoreDependencies: false);
        }

        public virtual void AddPackageReference(string packageId, Version version, bool ignoreDependencies) {
            IPackage package = PackageHelper.ResolvePackage(SourceRepository, LocalRepository, packageId, version);

            AddPackageReference(package, ignoreDependencies);
        }

        public virtual void AddPackageReference(IPackage package, bool ignoreDependencies) {
            Execute(package, new ProjectInstallWalker(LocalRepository,
                                                      SourceRepository,
                                                      new DependentsWalker(LocalRepository),
                                                      NullLogger.Instance,
                                                      ignoreDependencies));
        }

        private void Execute(IPackage package, IPackageOperationResolver resolver) {
            IEnumerable<PackageOperation> operations = resolver.ResolveOperations(package);
            if (operations.Any()) {
                foreach (PackageOperation operation in operations) {
                    Execute(operation);
                }
            }
            else if (LocalRepository.Exists(package)) {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, package.GetFullName());
            }
        }

        protected void Execute(PackageOperation operation) {
            bool packageExists = LocalRepository.Exists(operation.Package);

            if (operation.Action == PackageAction.Install) {
                // If the package is already installed, then skip it
                if (packageExists) {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, operation.Package.GetFullName());
                }
                else {
                    AddPackageReferenceToProject(operation.Package);
                }
            }
            else {
                if (packageExists) {
                    RemovePackageReferenceFromProject(operation.Package);
                }
            }
        }

        protected void AddPackageReferenceToProject(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnPackageReferenceAdding(args);

            if (args.Cancel) {
                return;
            }

            ExtractPackageFilesToProject(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_SuccessfullyAddedPackageReference, package.GetFullName(), Project.ProjectName);
            OnPackageReferenceAdded(args);
        }

        protected virtual void ExtractPackageFilesToProject(IPackage package) {
            // BUG 491: Installing a package with incompatible binaries still does a partial install.
            // Resolve assembly references first so that if this fails we never do anything to the project
            IEnumerable<IPackageAssemblyReference> assemblyReferences = Project.GetCompatibleItems(package.AssemblyReferences, NuGetResources.AssemblyReferences);
            IEnumerable<FrameworkAssemblyReference> frameworkReferences = Project.GetCompatibleItems(package.FrameworkAssemblies, NuGetResources.FrameworkAssemblies);

            try {
                // Add content files
                Project.AddFiles(package.GetContentFiles(), _fileTransformers);

                // Add the references to the reference path
                foreach (IPackageAssemblyReference assemblyReference in assemblyReferences) {
                    // Get the physical path of the assembly reference
                    string referencePath = Path.Combine(PathResolver.GetInstallPath(package), assemblyReference.Path);
                    string relativeReferencePath = PathUtility.GetRelativePath(Project.Root, referencePath);

                    if (Project.ReferenceExists(assemblyReference.Name)) {
                        Project.RemoveReference(assemblyReference.Name);
                    }

                    using (Stream stream = assemblyReference.GetStream()) {
                        Project.AddReference(relativeReferencePath, stream);
                    }
                }

                // Add GAC/Framework references
                foreach (FrameworkAssemblyReference frameworkReference in frameworkReferences) {
                    if (!Project.ReferenceExists(frameworkReference.AssemblyName)) {
                        Project.AddFrameworkReference(frameworkReference.AssemblyName);
                    }
                }
            }
            finally {
                // Add package to local repository in the finally so that the user can uninstall it
                // if any exception occurs. This is easier than rolling back since the user can just
                // manually uninstall things that may have failed.
                // If this fails then the user is out of luck.
                LocalRepository.AddPackage(package);
            }
        }

        public bool IsInstalled(IPackage package) {
            return LocalRepository.Exists(package);
        }

        public void RemovePackageReference(string packageId) {
            RemovePackageReference(packageId, forceRemove: false, removeDependencies: false);
        }

        public void RemovePackageReference(string packageId, bool forceRemove) {
            RemovePackageReference(packageId, forceRemove: forceRemove, removeDependencies: false);
        }

        public void RemovePackageReference(string packageId, bool forceRemove, bool removeDependencies) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = LocalRepository.FindPackage(packageId);

            if (package == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            RemovePackageReference(package, forceRemove, removeDependencies);
        }

        public virtual void RemovePackageReference(IPackage package, bool forceRemove, bool removeDependencies) {
            Execute(package, new UninstallWalker(LocalRepository,
                                                 new DependentsWalker(LocalRepository),
                                                 NullLogger.Instance,
                                                 removeDependencies,
                                                 forceRemove));
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void RemovePackageReferenceFromProject(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnPackageReferenceRemoving(args);

            if (args.Cancel) {
                return;
            }

            // Get other packages
            IEnumerable<IPackage> otherPackages = from p in LocalRepository.GetPackages()
                                                  where p.Id != package.Id
                                                  select p;

            // Get other references
            var otherAssemblyReferences = from p in otherPackages
                                          let assemblyReferences = Project.GetCompatibleItemsCore(p.AssemblyReferences)
                                          from assemblyReference in assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>() // This can happen if package installed left the project in a bad state
                                          select assemblyReference;

            // Get content files from other packages
            // Exclude transform files since they are treated specially
            var otherContentFiles = from p in otherPackages
                                    from file in p.GetContentFiles()
                                    where !_fileTransformers.ContainsKey(Path.GetExtension(file.Path))
                                    select file;

            // Get the files and references for this package, that aren't in use by any other packages so we don't have to do reference counting
            var assemblyReferencesToDelete = Project.GetCompatibleItemsCore(package.AssemblyReferences).Except(otherAssemblyReferences, PackageFileComparer.Default);
            var contentFilesToDelete = package.GetContentFiles()
                                              .Except(otherContentFiles, PackageFileComparer.Default);

            // Delete the content files
            Project.DeleteFiles(contentFilesToDelete, otherPackages, _fileTransformers);

            // Remove references
            foreach (IPackageAssemblyReference assemblyReference in assemblyReferencesToDelete) {
                Project.RemoveReference(assemblyReference.Name);
            }

            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_SuccessfullyRemovedPackageReference, package.GetFullName(), Project.ProjectName);
            OnPackageReferenceRemoved(args);
        }

        public void UpdatePackageReference(string packageId) {
            UpdatePackageReference(packageId, version: null, updateDependencies: true);
        }

        public void UpdatePackageReference(string packageId, Version version) {
            UpdatePackageReference(packageId, version: version, updateDependencies: true);
        }

        public virtual void UpdatePackageReference(string packageId, Version version, bool updateDependencies) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage oldPackage = LocalRepository.FindPackage(packageId);

            // Check to see if this package is installed
            if (oldPackage == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.ProjectDoesNotHaveReference, Project.ProjectName, packageId));
            }

            Logger.Log(MessageLevel.Debug, NuGetResources.Debug_LookingForUpdates, packageId);

            IPackage package = SourceRepository.FindPackage(packageId, version: version);

            if (package != null && oldPackage.Version != package.Version) {
                UpdatePackageReference(package, updateDependencies);
            }
            else {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_NoUpdatesAvailable, packageId);
            }
        }

        protected void UpdatePackageReference(IPackage package) {
            UpdatePackageReference(package, updateDependencies: true);
        }

        protected void UpdatePackageReference(IPackage package, bool updateDependencies) {
            AddPackageReference(package, !updateDependencies);
        }

        private void OnPackageReferenceAdding(PackageOperationEventArgs e) {
            if (_packageReferenceAdding != null) {
                _packageReferenceAdding(this, e);
            }
        }

        private void OnPackageReferenceAdded(PackageOperationEventArgs e) {
            if (_packageReferenceAdded != null) {
                _packageReferenceAdded(this, e);
            }
        }

        private void OnPackageReferenceRemoved(PackageOperationEventArgs e) {
            if (_packageReferenceRemoved != null) {
                _packageReferenceRemoved(this, e);
            }
        }

        private void OnPackageReferenceRemoving(PackageOperationEventArgs e) {
            if (_packageReferenceRemoving != null) {
                _packageReferenceRemoving(this, e);
            }
        }

        private PackageOperationEventArgs CreateOperation(IPackage package) {
            return new PackageOperationEventArgs(package, PathResolver.GetInstallPath(package));
        }

        private static IDictionary<XName, Action<XElement, XElement>> GetConfigMappings() {
            // REVIEW: This might be an edge case, but we're setting this rule for all xml files.
            // If someone happens to do a transform where the xml file has a configSections node
            // we will add it first. This is probably fine, but this is a config specific scenario
            return new Dictionary<XName, Action<XElement, XElement>>() {
                { "configSections" , (parent, element) => parent.AddFirst(element) }
            };
        }

        private class PackageFileComparer : IEqualityComparer<IPackageFile> {
            internal static PackageFileComparer Default = new PackageFileComparer();
            private PackageFileComparer() {
            }

            public bool Equals(IPackageFile x, IPackageFile y) {
                return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(IPackageFile obj) {
                return obj.Path.GetHashCode();
            }
        }

        // HACK: We need this to avoid a partial trust issue. We need to be able to evaluate closures
        // within this class
        internal static object Eval(FieldInfo fieldInfo, object obj) {
            return fieldInfo.GetValue(obj);
        }
    }
}
