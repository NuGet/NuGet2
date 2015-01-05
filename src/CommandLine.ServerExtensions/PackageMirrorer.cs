using System;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.ServerExtensions
{
    public enum MirrorDependenciesMode
    {
        Mirror,
        Ignore,
        Fail
    }

    public class PackageMirrorer
    {
        private ILogger _logger;

        public PackageMirrorer(IPackageRepository sourceRepository, IPackageRepository targetRepository)
        {
            if (sourceRepository == null)
            {
                throw new ArgumentNullException("sourceRepository");
            }

            if (targetRepository == null)
            {
                throw new ArgumentNullException("targetRepository");
            }

            SourceRepository = sourceRepository;
            TargetRepository = targetRepository;
        }

        public IPackageRepository SourceRepository
        {
            get;
            private set;
        }

        public IPackageRepository TargetRepository
        {
            get;
            private set;
        }

        public ILogger Logger
        {
            get
            {
                return _logger ?? NullLogger.Instance;
            }
            set
            {
                _logger = value;
            }
        }

        public bool NoOp
        {
            get;
            set;
        }

        public int MirrorPackage(string packageId, SemanticVersion version, bool allowPrereleaseVersions, MirrorDependenciesMode mirrorDependenciesMode)
        {
            IPackage package = PackageRepositoryHelper.ResolvePackage(SourceRepository, TargetRepository, packageId, version, allowPrereleaseVersions);

            return MirrorPackage(package, allowPrereleaseVersions, mirrorDependenciesMode);
        }

        public int MirrorPackage(IPackage package, bool allowPrereleaseVersions, MirrorDependenciesMode mirrorDependenciesMode)
        {
            return MirrorPackage(package, targetFramework: null, allowPrereleaseVersions: allowPrereleaseVersions, mirrorDependenciesMode: mirrorDependenciesMode);
        }

        public int MirrorPackage(IPackage package, FrameworkName targetFramework, bool allowPrereleaseVersions, MirrorDependenciesMode mirrorDependenciesMode)
        {
            var repo = mirrorDependenciesMode == MirrorDependenciesMode.Fail ? 
                TargetRepository : 
                SourceRepository;
            return Execute(package, new InstallWalker(
                TargetRepository,
                dependencyResolver: new DependencyResolverFromRepo(repo),
                targetFramework: targetFramework,
                logger: Logger,
                ignoreDependencies: mirrorDependenciesMode == MirrorDependenciesMode.Ignore,
                allowPrereleaseVersions: allowPrereleaseVersions,
                dependencyVersion: DependencyVersion.Lowest));
        }

        private int Execute(IPackage package, IPackageOperationResolver resolver)
        {
            var packagesToMirror = resolver.ResolveOperations(package)
                                           .Where(o => o.Action == PackageAction.Install)
                                           .Select(o => o.Package)
                                           .ToList();

            int countMirrored = 0;
            foreach (var p in packagesToMirror)
            {
                if (TargetRepository.Exists(package))
                {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyPresent, p.GetFullName(), TargetRepository.Source);
                }
                else
                {
                    ExecuteMirror(p);
                    countMirrored++;
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageMirroredSuccessfully, p.GetFullName(), TargetRepository.Source);
                }
            }

            return countMirrored;
        }

        private void ExecuteMirror(IPackage package)
        {
            if (! NoOp)
            {
                TargetRepository.AddPackage(package);
            }
        }
    }
}
