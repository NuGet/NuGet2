using System;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
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

        public virtual void MirrorPackage(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            IPackage package = PackageHelper.ResolvePackage(SourceRepository, TargetRepository, packageId, version, allowPrereleaseVersions);

            MirrorPackage(package, ignoreDependencies, allowPrereleaseVersions);
        }

        public virtual void MirrorPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            MirrorPackage(package, targetFramework: null, ignoreDependencies: ignoreDependencies, allowPrereleaseVersions: allowPrereleaseVersions);
        }

        protected void MirrorPackage(IPackage package, FrameworkName targetFramework, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            Execute(package, new InstallWalker(TargetRepository,
                                               SourceRepository,
                                               targetFramework,
                                               Logger,
                                               ignoreDependencies,
                                               allowPrereleaseVersions));
        }

        private void Execute(IPackage package, IPackageOperationResolver resolver)
        {
            var operations = resolver.ResolveOperations(package);
            bool didOperation = false;                        
            foreach (PackageOperation operation in operations)
            {
                didOperation = true; // not using operations.Any() since this could cause multiple enumerations 
                Execute(operation);
            }
            if (!didOperation && TargetRepository.Exists(package))
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyPresent, package.GetFullName(), TargetRepository.Source);
            }
        }

        protected void Execute(PackageOperation operation)
        {
            bool packageExists = TargetRepository.Exists(operation.Package);

            // technically it's mirroring; however PackageAction enum currently has 2 entries only and is being treated throughout the rest of 
            // the code as a boolean (if statements vs. switch), making it a regression friendly object to extend
            if (operation.Action == PackageAction.Install)
            {
                if (packageExists)
                {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageAlreadyPresent, operation.Package.GetFullName(), TargetRepository.Source);
                }
                else
                {
                    ExecuteMirror(operation.Package);
                }
            }
            else
            {
                throw new NotSupportedException("How did I get here?");
            }
        }

        protected void ExecuteMirror(IPackage package)
        {
            if (!NoOp)
            {
                TargetRepository.AddPackage(package);
            }
            Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageMirroredSuccessfully, package.GetFullName(), TargetRepository.Source);
        }
    }
}
