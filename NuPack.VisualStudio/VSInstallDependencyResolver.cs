using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace NuPack.VisualStudio {
    public class VSUninstallDependencyResolver : UninstallWalker, IVsDependencyResolver {
        private readonly Dictionary<IPackage, List<IPackage>> _packagesLookup = new Dictionary<IPackage, List<IPackage>>();
        private Stack<PackageOperation> _solutionOperations;
        private Stack<PackageOperation> _projectOperations;

        public VSUninstallDependencyResolver(IPackageRepository repository,
                                             IDependentsResolver resolver,
                                             ILogger logger, bool removeDependencies, bool forceRemove)
            : base(repository, resolver, logger, removeDependencies, forceRemove) {
            _projectOperations = new Stack<PackageOperation>();
            _solutionOperations = new Stack<PackageOperation>();
        }

        public IEnumerable<PackageOperation> ProjectOperations {
            get {
                return _projectOperations;
            }
        }

        public IEnumerable<PackageOperation> SolutionOperations {
            get {
                return _solutionOperations;
            }
        }

        protected override void OnBeforeDependencyWalk(IPackage package) {
            _packagesLookup[package] = new List<IPackage>();
            base.OnBeforeDependencyWalk(package);
        }

        protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            _packagesLookup[package].Add(dependency);
            return base.OnAfterResolveDependency(package, dependency);
        }

        public void Resolve(IPackage package) {
            _packagesLookup.Clear();

            var operationLookup = new Dictionary<IPackage, PackageOperation>();
            foreach (var operation in ResolveOperations(package)) {
                operationLookup[operation.Package] = operation;
            }

            VSDependencyResolverHelper.ResolvePackageTypes(this,
                                                          package,
                                                          p => _packagesLookup[p],
                                                          p => operationLookup[p]);
        }

        public void AddSolutionOperation(PackageOperation operation) {
            _solutionOperations.Push(operation);
        }

        public void AddProjectOperation(PackageOperation operation) {
            _projectOperations.Push(operation);
        }
    }

    public class VSInstallDependencyResolver : InstallWalker, IVsDependencyResolver {
        private readonly Dictionary<IPackage, List<IPackage>> _packagesLookup = new Dictionary<IPackage, List<IPackage>>();
        private IList<PackageOperation> _solutionOperations;
        private IList<PackageOperation> _projectOperations;

        public VSInstallDependencyResolver(IPackageRepository localRepository,
                                    IPackageRepository sourceRepository,
                                    ILogger logger,
                                    bool ignoreDependencies)
            : base(localRepository, sourceRepository, logger, ignoreDependencies) {
            _solutionOperations = new List<PackageOperation>();
            _projectOperations = new List<PackageOperation>();
        }

        public IEnumerable<PackageOperation> ProjectOperations {
            get {
                return _projectOperations;
            }
        }

        public IEnumerable<PackageOperation> SolutionOperations {
            get {
                return _solutionOperations;
            }
        }

        protected override void OnBeforeDependencyWalk(IPackage package) {
            _packagesLookup[package] = new List<IPackage>();
            base.OnBeforeDependencyWalk(package);
        }

        protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            // Add the resolved dependency to the list
            _packagesLookup[package].Add(dependency);
            return base.OnAfterResolveDependency(package, dependency);
        }


        public void Resolve(IPackage package) {
            _packagesLookup.Clear();


            var operationLookup = new Dictionary<IPackage, PackageOperation>();
            foreach (var operation in ResolveOperations(package)) {
                operationLookup[operation.Package] = operation;
            }

            VSDependencyResolverHelper.ResolvePackageTypes(this,
                                                          package,
                                                          p => _packagesLookup[p],
                                                          p => operationLookup[p]);
        }


        public void AddSolutionOperation(PackageOperation operation) {
            _solutionOperations.Add(operation);
        }

        public void AddProjectOperation(PackageOperation operation) {
            _projectOperations.Add(operation);
        }
    }

}
