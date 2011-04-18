using System;
using System.Linq;

namespace NuGet.Common {
    public class CommandLineRepositoryFactory : IPackageRepositoryFactory {
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public CommandLineRepositoryFactory()
            : this(PackageRepositoryFactory.Default) {
        }

        public CommandLineRepositoryFactory(IPackageRepositoryFactory repositoryFactory) {
            _repositoryFactory = repositoryFactory;
        }

        public IPackageRepository CreateRepository(PackageSource packageSource) {
            return new LazyRepository(_repositoryFactory, packageSource);
        }

        private class LazyRepository : IPackageRepository {
            private const string UserAgentClient = "NuGet Command Line";
            private readonly Lazy<IPackageRepository> _repository;

            public LazyRepository(IPackageRepositoryFactory repositoryFactory, PackageSource packageSource) {
                _repository = new Lazy<IPackageRepository>(() => CreateRepository(repositoryFactory, packageSource));
            }

            private static IPackageRepository CreateRepository(IPackageRepositoryFactory repositoryFactory, PackageSource packageSource) {
                IPackageRepository packageRepository = repositoryFactory.CreateRepository(packageSource);
                var httpClientEvents = packageRepository as IHttpClientEvents;

                if (httpClientEvents != null) {
                    httpClientEvents.SendingRequest += (sender, args) => {
                        string userAgent = HttpUtility.CreateUserAgentString(UserAgentClient);
                        HttpUtility.SetUserAgent(args.Request, userAgent);
                    };
                }

                return packageRepository;
            }

            public string Source {
                get { return _repository.Value.Source; }
            }

            public IQueryable<IPackage> GetPackages() {
                return _repository.Value.GetPackages();
            }

            public void AddPackage(IPackage package) {
                _repository.Value.AddPackage(package);
            }

            public void RemovePackage(IPackage package) {
                _repository.Value.RemovePackage(package);
            }
        }
    }
}