namespace NuPack {
    using System;

    public class UpdateDependencyResolver : IUpdateDependencyResolver {
        public UpdateDependencyResolver(IPackageRepository localRepository,
                                        IPackageRepository sourceRepository,
                                        IDependencyResolver dependentsResolver,
                                        ILogger logger,
                                        bool updateDependencies) {
            if (localRepository == null) {
                throw new ArgumentNullException("localRepository");
            }
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (dependentsResolver == null) {
                throw new ArgumentNullException("dependentsResolver");
            }
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }
            LocalRepository = localRepository;
            SourceRepository = sourceRepository;
            DependentsResolver = dependentsResolver;
            Logger = logger;
            UpdateDependencies = updateDependencies;
        }

        public IPackageRepository LocalRepository {
            get;
            private set;
        }

        public IPackageRepository SourceRepository {
            get;
            private set;
        }

        public IDependencyResolver DependentsResolver {
            get;
            private set;
        }

        public ILogger Logger {
            get;
            private set;
        }

        public bool UpdateDependencies {
            get;
            private set;
        }

        public PackagePlan ResolveDependencies(IPackage oldPackage, IPackage newPackage) {
            // When we go to update a package to a newer version, we get the install and uninstall plans then do a diff to see
            // what really needs to be uninstalled and installed.
            // If we had the following structure and we were updating from A 1.0 to A 2.0:
            // A 1.0 -> [B 1.0, C 1.0]
            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            // We will end up uninstalling C 1.0 and installing A 2.0, C 2.0 and D 1.0

            var uninstallWalker = new ProjectUpdateUninstallWalker(LocalRepository,
                                                                   DependentsResolver,
                                                                   Logger,
                                                                   UpdateDependencies,
                                                                   forceRemove: false);

            uninstallWalker.Walk(oldPackage);

            var installWalker = new ProjectUpdateInstallWalker(uninstallWalker.Packages,
                                                               LocalRepository,
                                                               SourceRepository,
                                                               DependentsResolver,
                                                               Logger,
                                                               !UpdateDependencies);

            installWalker.Walk(newPackage);

            return new PackagePlan(installWalker.Packages, uninstallWalker.Packages);
        }
    }
}
