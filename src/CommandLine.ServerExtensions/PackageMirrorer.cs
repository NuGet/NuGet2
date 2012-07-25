using System;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.ServerExtensions
{
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

        public bool MirrorPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            IPackage package = PackageHelper.ResolvePackage(SourceRepository, TargetRepository, packageId, version, allowPrereleaseVersions);

            return MirrorPackage(package, ignoreDependencies, allowPrereleaseVersions);
        }

        public bool MirrorPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            return MirrorPackage(package, targetFramework: null, ignoreDependencies: ignoreDependencies, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        public bool MirrorPackage(IPackage package, FrameworkName targetFramework, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            return Execute(package, new InstallWalker(TargetRepository,
                                               SourceRepository,
                                               targetFramework,
                                               Logger,
                                               ignoreDependencies,
                                               allowPrereleaseVersions));
        }

        private bool Execute(IPackage package, IPackageOperationResolver resolver)
        {
            var packagesToMirror = resolver.ResolveOperations(package)
                                           .Where(o => o.Action == PackageAction.Install)
                                           .Select(o => o.Package)
                                           .ToList();

            bool mirrored = false;
            foreach (var p in packagesToMirror)
            {
                if (TargetRepository.Exists(package))
                {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyPresent, p.GetFullName(), TargetRepository.Source);
                }
                else
                {
                    ExecuteMirror(p);
                    mirrored = true;
                }
            }

            if (mirrored)
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyPresent, package.GetFullName(), TargetRepository.Source);
            }
            return mirrored;
        }

        private void ExecuteMirror(IPackage package)
        {
            if (!NoOp)
            {
                TargetRepository.AddPackage(package);
            }
            Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageMirroredSuccessfully, package.GetFullName(), TargetRepository.Source);
        }
    }
}
