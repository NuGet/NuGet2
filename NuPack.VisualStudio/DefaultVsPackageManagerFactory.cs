using System;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class DefaultVsPackageManagerFactory : IVsPackageManagerFactory {
        private static readonly Lazy<DefaultVsPackageManagerFactory> _instance = new Lazy<DefaultVsPackageManagerFactory>(() => 
            new DefaultVsPackageManagerFactory(DTEExtensions.DTE, CachedRepositoryFactory.Instance));
        
        private readonly DTE _dte;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public DefaultVsPackageManagerFactory(DTE dte, IPackageRepositoryFactory repositoryFactory) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            _dte = dte;
            _repositoryFactory = repositoryFactory;
        }

        public static IVsPackageManagerFactory Instance {
            get {
                return _instance.Value;
            }
        }

        public IVsPackageManager CreatePackageManager() {
            return new VsPackageManager(_dte);
        }

        public IVsPackageManager CreatePackageManager(string source) {
            return new VsPackageManager(_dte, _repositoryFactory.CreateRepository(source));
        }
    }
}
