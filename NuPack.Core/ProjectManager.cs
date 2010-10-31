namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
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

        public ProjectManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, ProjectSystem project) :
            this(sourceRepository,
                 pathResolver,
                 project,
                 new PackageReferenceRepository(project, sourceRepository)) {
        }

        public ProjectManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, ProjectSystem project, IPackageRepository localRepository) {
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

        public ProjectSystem Project {
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
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            IPackage package = SourceRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.UnknownPackage, packageId));
            }

            AddPackageReference(package, ignoreDependencies);
        }

        protected virtual void AddPackageReference(IPackage package, bool ignoreDependencies) {
            Execute(package, new ProjectInstallWalker(LocalRepository,
                                                      SourceRepository,
                                                      new DependentsWalker(LocalRepository),
                                                      NullLogger.Instance,
                                                      ignoreDependencies));
        }

        private void Execute(IPackage package, IPackageOperationResolver resolver) {
            foreach (PackageOperation operation in resolver.ResolveOperations(package)) {
                Execute(operation);
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

            // Resolve assembly references
            var assemblyReferences = ResolveAssemblyReferences(package);

            // Add content files
            Project.AddFiles(package.GetContentFiles(), _fileTransformers);

            // Add the references to the reference path
            foreach (IPackageAssemblyReference assemblyReference in assemblyReferences) {
                // Get teh physical path of the assembly reference
                string referencePath = Path.Combine(PathResolver.GetInstallPath(package), assemblyReference.Path);

                // If this assembly is already referenced by the project then skip it
                if (Project.ReferenceExists(assemblyReference.Name)) {
                    Logger.Log(MessageLevel.Warning, NuGetResources.Warning_AssemblyAlreadyReferenced, Project.ProjectName, assemblyReference.Name);
                    continue;
                }

                Project.AddReference(referencePath);
            }

            // Add package to local repository
            LocalRepository.AddPackage(package);

            Logger.Log(MessageLevel.Info, NuGetResources.Log_SuccessfullyAddedPackageReference, package.GetFullName(), Project.ProjectName);
            OnPackageReferenceAdded(args);
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

        protected virtual void RemovePackageReference(IPackage package, bool force, bool removeDependencies) {
            Execute(package, new UninstallWalker(LocalRepository,
                                                 new DependentsWalker(LocalRepository),
                                                 NullLogger.Instance,
                                                 removeDependencies,
                                                 force));
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void RemovePackageReferenceFromProject(IPackage package) {
            PackageOperationEventArgs args = CreateOperation(package);
            OnPackageReferenceRemoving(args);

            if (args.Cancel) {
                return;
            }

            // Get other packages
            var otherPackages = from p in LocalRepository.GetPackages()
                                where p.Id != package.Id
                                select p;

            // Get other references
            var otherAssemblyReferences = from p in otherPackages
                                          let assemblyReferences = GetCompatibleAssemblyReferences(Project.TargetFramework, p.AssemblyReferences)
                                          from assemblyReference in assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>() // This can happen if package installed left the project in a bad state
                                          select assemblyReference;

            // Get content files from other packages
            // Exclude transform files since they are treated specially
            var otherContentFiles = from p in otherPackages
                                    from file in p.GetContentFiles()
                                    where !_fileTransformers.ContainsKey(Path.GetExtension(file.Path))
                                    select file;

            // Get the files and references for this package, that aren't in use by any other packages so we don't have to do reference counting
            var assemblyReferencesToDelete = package.AssemblyReferences.Except(otherAssemblyReferences, PackageFileComparer.Default);
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

            // Check to see if this package is installed
            if (!LocalRepository.Exists(packageId)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuGetResources.ProjectDoesNotHaveReference, Project.ProjectName, packageId));
            }

            Logger.Log(MessageLevel.Debug, NuGetResources.Debug_LookingForUpdates, packageId);

            IPackage package = SourceRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_NoUpdatesAvailable, packageId);
            }
            else {
                UpdatePackageReference(package, updateDependencies);
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

        private IEnumerable<IPackageAssemblyReference> ResolveAssemblyReferences(IPackage package) {
            // A package might have references that target a specific version of the framework (.net/silverlight etc)
            // so we try to get the highest version that satifies the target framework i.e.
            // if a package has 1.0, 2.0, 4.0 and the target framework is 3.5 we'd pick the 2.0 references.
            var compatibleAssemblyReferences = GetCompatibleAssemblyReferences(Project.TargetFramework, package.AssemblyReferences);

            if (compatibleAssemblyReferences == null) {
                throw new InvalidOperationException(
                           String.Format(CultureInfo.CurrentCulture,
                           NuGetResources.UnableToFindCompatibleReference, Project.TargetFramework));
            }

            return compatibleAssemblyReferences;
        }

        internal static IEnumerable<IPackageAssemblyReference> GetCompatibleAssemblyReferences(FrameworkName projectFramework, IEnumerable<IPackageAssemblyReference> allAssemblyReferences) {
            if (!allAssemblyReferences.Any()) {
                return Enumerable.Empty<IPackageAssemblyReference>();
            }

            // Default framework for assembly references with an unspecified framework name
            // always match the project framework's identifier by is the lowest possible version
            var defaultFramework = new FrameworkName(projectFramework.Identifier, new Version());

            // Group references by target framework (if there is no target framework we assume it is the default)
            var frameworkGroups = allAssemblyReferences.GroupBy(g => g.TargetFramework ?? defaultFramework);

            // Try to find the best match
            return (from g in frameworkGroups
                    where g.Key.Identifier.Equals(projectFramework.Identifier, StringComparison.OrdinalIgnoreCase) &&
                          g.Key.Version <= projectFramework.Version
                    orderby g.Key.Version descending
                    select g).FirstOrDefault();
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
    }
}
