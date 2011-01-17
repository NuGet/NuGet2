using System;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [Export(typeof(IPackageRepository))]
    public class VsPackageSourceRepository : IPackageRepository {
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        [ImportingConstructor]
        public VsPackageSourceRepository(IPackageRepositoryFactory repositoryFactory, 
                                         IPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public string Source {
            get {
                return ActiveRepository.Source;
            }
        }

        private IPackageRepository ActiveRepository {
            get {
                if (_packageSourceProvider.ActivePackageSource == null) {
                    throw new InvalidOperationException(VsResources.NoActivePackageSource);
                }
                return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource);
            }
        }

        public IQueryable<IPackage> GetPackages() {
            return ActiveRepository.GetPackages();
        }

        public IPackage FindPackage(string packageId, Version version) {
            return ActiveRepository.FindPackage(packageId, version);
        }

        public void AddPackage(IPackage package) {
            ActiveRepository.AddPackage(package);
        }

        public void RemovePackage(IPackage package) {
            ActiveRepository.RemovePackage(package);
        }
    }
}
