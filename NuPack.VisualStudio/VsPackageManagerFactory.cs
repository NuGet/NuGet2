using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageManagerFactory))]
    public class VsPackageManagerFactory : IVsPackageManagerFactory {
        private const string SolutionRepositoryDirectory = "packages";

        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly ISolutionManager _solutionManager;
        private readonly DTE _dte;
        private readonly IComponentModel _componentModel;

        private IFileSystem _solutionFileSystem;
        private IPackageRepository _solutionRepository;

        [ImportingConstructor]
        public VsPackageManagerFactory(DTE dte,
                                       ISolutionManager solutionManager,
                                       IPackageRepositoryFactory repositoryFactory,
                                       IComponentModel componentModel) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (solutionManager == null) {
                throw new ArgumentNullException("solutionManager");
            }
            if (componentModel == null) {
                throw new ArgumentNullException("componentModel");
            }

            _componentModel = componentModel;
            _dte = dte;
            _solutionManager = solutionManager;
            _repositoryFactory = repositoryFactory;

            _solutionManager.SolutionClosing += (sender, e) => {
                _solutionFileSystem = null;
                _solutionRepository = null;
            };
        }

        private IFileSystem SolutionFileSystem {
            get {
                if (_solutionFileSystem == null) {
                    _solutionFileSystem = GetFileSystem();
                }
                return _solutionFileSystem;
            }
        }

        private IPackageRepository SolutionRepository {
            get {
                if (_solutionRepository == null) {
                    _solutionRepository = new LocalPackageRepository(new DefaultPackagePathResolver(SolutionFileSystem), SolutionFileSystem);
                }
                return _solutionRepository;
            }
        }

        public IVsPackageManager CreatePackageManager() {
            return CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>());
        }

        public IVsPackageManager CreatePackageManager(string source) {
            return CreatePackageManager(_repositoryFactory.CreateRepository(new PackageSource(source, source)));
        }

        public IVsPackageManager CreatePackageManager(IPackageRepository repository) {
            return new VsPackageManager(_solutionManager, repository, SolutionFileSystem, SolutionRepository);
        }

        private IFileSystem GetFileSystem() {
            // Get the source control providers
            var providers = _componentModel.GetExtensions<ISourceControlFileSystemProvider>();

            // Get the packages path
            string path = Path.Combine(Path.GetDirectoryName(_dte.Solution.FullName), "packages");
            IFileSystem fileSystem = null;

            var sourceControl = (SourceControl2)_dte.SourceControl;
            if (providers.Any() && sourceControl != null) {
                SourceControlBindings binding = null;
                try {
                    // Get the binding for this solution
                    binding = sourceControl.GetBindings(_dte.Solution.FullName);
                }
                catch (NotImplementedException) {
                    // Some source control providers don't bother to implement this.
                    // TFS might be the only one using it
                }

                if (binding != null) {
                    fileSystem = providers.Select(provider => GetFileSystemFromProvider(provider, path, binding))
                                          .Where(fs => fs != null)
                                          .FirstOrDefault();
                }
            }

            return fileSystem ?? new FileBasedProjectSystem(path);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static IFileSystem GetFileSystemFromProvider(ISourceControlFileSystemProvider provider, string path, SourceControlBindings binding) {
            try {
                return provider.GetFileSystem(path, binding);
            }
            catch {
                // Ignore exceptions that can happen when some binaries are missing. e.g. TfsSourceControlFileSystemProvider
                // would throw a jitting error if TFS is not installed
            }

            return null;
        }
    }
}
