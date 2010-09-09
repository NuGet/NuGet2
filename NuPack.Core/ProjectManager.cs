namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using Microsoft.Internal.Web.Utils;
    using NuPack.Resources;

    public class ProjectManager {
        private PackageEventListener _listener;
        private IDictionary<string, IPackageFileModifier> _modifiers = new Dictionary<string, IPackageFileModifier>(StringComparer.OrdinalIgnoreCase) {
            { ".transform", new XmlTransfomer() }
        };

        public ProjectManager(IPackageRepository sourceRepository, IPackageAssemblyPathResolver assemblyPathResolver, string projectRoot, string packageFileRoot = "")
            : this(sourceRepository, assemblyPathResolver, new FileBasedProjectSystem(projectRoot), packageFileRoot) {
        }

        public ProjectManager(IPackageRepository sourceRepository, IPackageAssemblyPathResolver assemblyPathResolver, ProjectSystem project, string packageFileRoot = "") {
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (assemblyPathResolver == null) {
                throw new ArgumentNullException("assemblyPathResolver");
            }
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            SourceRepository = sourceRepository;
            Project = project;
            AssemblyPathResolver = assemblyPathResolver;

            // REVIEW: We need a better abstraction for a package reference
            string packageFilePath = Path.Combine(packageFileRoot, PackageReferenceRepository.PackageFile);
            LocalRepository = new PackageReferenceRepository(Project, packageFilePath, SourceRepository);
        }

        private IPackageAssemblyPathResolver AssemblyPathResolver { get; set; }
        // REVIEW: Should we expose this?
        internal IPackageRepository LocalRepository { get; set; }

        public IPackageRepository SourceRepository { get; private set; }
        public ProjectSystem Project { get; private set; }

        public PackageEventListener Listener {
            get {
                return _listener ?? PackageEventListener.Default;
            }
            set {
                _listener = value;
                Project.Listener = value;
            }
        }

        public virtual void AddPackageReference(string packageId, Version version = null, bool ignoreDependencies = false) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            Package package = SourceRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }

            if (!package.HasProjectContent) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuPackResources.PackageHasNoProjectContent, package));
            }

            AddPackageReference(package, ignoreDependencies);
        }

        private void AddPackageReference(Package package, bool ignoreDependencies) {
            // If there is a package with this id already referenced attempt an update
            if (LocalRepository.IsPackageInstalled(package.Id) && TryUpdate(package)) {
                return;
            }

            IEnumerable<Package> packages = null;

            if (ignoreDependencies) {
                packages = new[] { package };
            }
            else {
                packages = DependencyManager.ResolveDependenciesForProjectInstall(package, LocalRepository, SourceRepository, Listener);
            }

            VerifyInstall(packages);

            AddPackageReferencesToProject(packages);
        }

        private bool TryUpdate(Package package) {
            // Get the currently referenced package
            Package referencedPackage = LocalRepository.FindPackage(package.Id);

            // If it's a lower version then throw raise an error
            if (package.Version < referencedPackage.Version) {
                throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.NewerVersionAlreadyReferenced, Project.ProjectName, package.Id));
            }

            // If the package is installed (i.e the package an all of it's dependencies)
            IEnumerable<Package> referencedPackageWithDependencies = DependencyManager.ResolveDependenciesForInstall(referencedPackage,
                                                                                                                     LocalRepository,
                                                                                                                     SourceRepository,
                                                                                                                     Listener);
            VerifyInstall(referencedPackageWithDependencies);

            // Everything is installed
            if (referencedPackageWithDependencies.All(LocalRepository.IsPackageInstalled)) {
                // If they aren't the same version then do nothing
                if (!referencedPackage.Version.Equals(package.Version)) {
                    // Then try to do an update to the one we specified
                    UpdatePackageReference(package);
                }
                else {
                    Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, package);
                }

                return true;
            }

            return false;
        }

        private void AddPackageReferencesToProject(IEnumerable<Package> packages) {
            Debug.Assert(packages != null, "packages shouldn't be null");

            foreach (Package package in packages) {
                // If the package is already installed, then skip it
                if (LocalRepository.IsPackageInstalled(package)) {
                    // TODO: Change messages
                    Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, package);
                    continue;
                }

                AddPackageReferenceToProject(package);
            }
        }

        protected void AddPackageReferenceToProject(Package package) {
            // Add package to local repository
            LocalRepository.AddPackage(package);

            // Resolve assembly references
            var assemblyReferences = ResolveAssemblyReferences(package);

            // Add content files
            Project.AddFiles(package.GetContentFiles(),
                             Listener,
                             ExecuteModify,
                             ResolvePath);

            // Add the references to the reference path
            foreach (IPackageAssemblyReference assemblyReference in assemblyReferences) {
                // Get teh physical path of the assembly reference
                string referencePath = AssemblyPathResolver.GetAssemblyPath(package, assemblyReference);

                Project.AddReference(referencePath);
            }

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_SuccessfullyAddedPackageReference, package, Project.ProjectName);
        }

        private string ResolvePath(string path) {
            // Return empty string for the content directory
            if (path.Equals("content", StringComparison.OrdinalIgnoreCase)) {
                return String.Empty;
            }
            return path.Substring(@"content\".Length);
        }

        private bool ExecuteModify(IPackageFile file) {
            string extension = Path.GetExtension(file.Path);
            IPackageFileModifier modifier;
            if (_modifiers.TryGetValue(extension, out modifier)) {
                modifier.Modify(file, Project);
                return true;
            }
            return false;
        }

        private void ExecuteRevert(IPackageFile file, IEnumerable<IPackageFile> matchingFiles) {
            string extension = Path.GetExtension(file.Path);
            IPackageFileModifier modifier;
            if (_modifiers.TryGetValue(extension, out modifier)) {
                modifier.Revert(file, matchingFiles, Project);
            }
        }

        public void RemovePackageReference(string packageId, bool force = false, bool removeDependencies = false) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            Package package = LocalRepository.FindPackage(packageId);

            if (package == null) {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    NuPackResources.UnknownPackage, packageId));
            }

            RemovePackageReference(package, force, removeDependencies);
        }

        protected virtual void RemovePackageReference(Package package, bool force, bool removeDependencies) {
            IEnumerable<Package> packages = DependencyManager.ResolveDependenciesForProjectUninstall(package, LocalRepository, force, removeDependencies, Listener);

            RemovePackageReferencesFromProject(packages);
        }

        private void RemovePackageReferencesFromProject(IEnumerable<Package> packages) {
            Debug.Assert(packages != null, "packages should not be null");

            foreach (var package in packages) {
                RemovePackageReferenceFromProject(package);
            }
        }

        private void RemovePackageReferenceFromProject(Package package) {
            // Get other packages
            var otherPackages = from p in LocalRepository.GetPackages()
                                where p.Id != package.Id
                                select p;

            // Get other references
            var otherAssemblyReferences = from p in otherPackages
                                          let assemblyReferences = GetCompatibleAssemblyReferences(Project.TargetFramework, p.AssemblyReferences)
                                          from assemblyReference in assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>() // This can happen if package installed left the project in a bad state
                                          select assemblyReference;

            // Get other files
            var otherContentFiles = from p in otherPackages
                                    from file in p.GetContentFiles()
                                    select file;

            // Get the files and references for this package, that aren't in use by any other packages so we don't have to do reference counting
            var assemblyReferencesToDelete = package.AssemblyReferences.Except(otherAssemblyReferences, PackageFileComparer.Default);
            var contentFilesToDelete = package.GetContentFiles().Except(otherContentFiles, PackageFileComparer.Default);

            // Delete the content files
            Project.DeleteFiles(contentFilesToDelete,
                                Listener,
                                file => ExecuteRevert(file, from p in otherPackages
                                                            from otherFile in p.GetContentFiles()
                                                            where otherFile.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)
                                                            select otherFile),
                                ResolvePath);

            // Remove references
            foreach (IPackageAssemblyReference assemblyReference in assemblyReferencesToDelete) {
                Project.RemoveReference(assemblyReference.Name);
            }

            // Remove package to the repository
            LocalRepository.RemovePackage(package);

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_SuccessfullyRemovedPackageReference, package, Project.ProjectName);
        }

        public virtual void UpdatePackageReference(string packageId, Version version = null, bool updateDependencies = true) {
            if (String.IsNullOrEmpty(packageId)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            // Check to see if this package is installed
            if (!LocalRepository.IsPackageInstalled(packageId)) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    NuPackResources.ProjectDoesNotHaveReference, Project.ProjectName, packageId));
            }

            Listener.OnReportStatus(StatusLevel.Debug, NuPackResources.Debug_LookingForUpdates, packageId);

            Package package = SourceRepository.FindPackage(packageId, exactVersion: version);

            if (package == null) {
                Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_NoUpdatesAvailable, packageId);
            }
            else {
                UpdatePackageReference(package, updateDependencies);
            }
        }


        protected void UpdatePackageReference(Package package, bool updateDependencies = true) {
            Debug.Assert(package != null, "package should not be null");

            // If the most up-to-date version is already installed then do nothing
            if (LocalRepository.IsPackageInstalled(package)) {
                Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_PackageUpToDate, package.Id);
                return;
            }

            // Get the installed package
            Package oldPackage = LocalRepository.FindPackage(package.Id);

            if (package.Version < oldPackage.Version) {
                throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture, NuPackResources.NewerVersionAlreadyReferenced, Project.ProjectName, package.Id));
            }

            Listener.OnReportStatus(StatusLevel.Info, NuPackResources.Log_UpdatingToSpecificVersion, oldPackage, package.Version);

            // When we go to update a package to a newer version, we get the install and uninstall plans then do a diff to see
            // what really needs to be uninstalled and installed.
            // If we had the following structure and we were updating from A 1.0 to A 2.0:
            // A 1.0 -> [B 1.0, C 1.0]
            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            // We will end up uninstalling C 1.0 and installing A 2.0, C 2.0 and D 1.0
            PackagePlan plan = DependencyManager.ResolveDependenciesForUpdate(oldPackage,
                                                                              package,
                                                                              LocalRepository,
                                                                              SourceRepository,
                                                                              Listener,
                                                                              updateDependencies);

            Execute(plan);
        }

        private void Execute(PackagePlan plan) {
            foreach (var package in plan.PackagesToUninstall) {
                RemovePackageReferenceFromProject(package);
            }

            foreach (var package in plan.PackagesToInstall) {
                AddPackageReferenceToProject(package);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        public IEnumerable<Package> GetPackageReferences() {
            return LocalRepository.GetPackages();
        }

        public Package GetPackageReference(string packageId) {
            return LocalRepository.FindPackage(packageId);
        }

        private void VerifyInstall(IEnumerable<Package> packages) {
            Debug.Assert(packages != null, "packages should not be null");

            foreach (Package package in packages) {
                // Check for installed packages with the same Id
                Package installedPackage = LocalRepository.FindPackage(package.Id);
                if (installedPackage != null) {
                    if (package.Version < installedPackage.Version) {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.CurrentCulture, NuPackResources.NewerVersionAlreadyReferenced, Project.ProjectName, package.Id));
                    }
                }
            }
        }

        private IEnumerable<IPackageAssemblyReference> ResolveAssemblyReferences(Package package) {
            // A package might have references that target a specific version of the framework (.net/silverlight etc)
            // so we try to get the highest version that satifies the target framework i.e.
            // if a package has 1.0, 2.0, 4.0 and the target framework is 3.5 we'd pick the 2.0 references.
            var compatibleAssemblyReferences = GetCompatibleAssemblyReferences(Project.TargetFramework, package.AssemblyReferences);

            if (compatibleAssemblyReferences == null) {
                throw new InvalidOperationException(
                           String.Format(CultureInfo.CurrentCulture,
                           NuPackResources.UnableToFindCompatibleReference, Project.TargetFramework));
            }

            return compatibleAssemblyReferences;
        }

        internal static IEnumerable<IPackageAssemblyReference> GetCompatibleAssemblyReferences(FrameworkName projectFramework, IEnumerable<IPackageAssemblyReference> allAssemblyReferences) {
            if (!allAssemblyReferences.Any()) {
                return Enumerable.Empty<IPackageAssemblyReference>();
            }

            // Group references by target framework (if there is no target framework we assume it is the same as the project framework)
            var frameworkGroups = allAssemblyReferences.GroupBy(g => g.TargetFramework ?? projectFramework);

            return (from g in frameworkGroups
                    where g.Key.Identifier.Equals(projectFramework.Identifier, StringComparison.OrdinalIgnoreCase) &&
                          g.Key.Version <= projectFramework.Version
                    orderby g.Key.Version descending
                    select g).FirstOrDefault();
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