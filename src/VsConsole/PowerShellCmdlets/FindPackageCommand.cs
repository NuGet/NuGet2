using System;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// FindPackage is identical to GetPackage except that FindPackage filters packages only by Id and does not consider description or tags.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "Package", DefaultParameterSetName = "Default")]
    [OutputType(typeof(IPackage))]
    public class FindPackageCommand : GetPackageCommand
    {
        private const int MaxReturnedPackages = 30;

        public FindPackageCommand()
            : this(ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                   ServiceLocator.GetInstance<IVsPackageSourceProvider>(),
                   ServiceLocator.GetInstance<ISolutionManager>(),
                   ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                   ServiceLocator.GetInstance<IHttpClientEvents>())
        {
        }

        public FindPackageCommand(IPackageRepositoryFactory repositoryFactory,
                          IVsPackageSourceProvider packageSourceProvider,
                          ISolutionManager solutionManager,
                          IVsPackageManagerFactory packageManagerFactory,
                          IHttpClientEvents httpClientEvents)
            : base(repositoryFactory, packageSourceProvider, solutionManager, packageManagerFactory, httpClientEvents, null)
        {
        }

        /// <summary>
        /// Determines if an exact Id match would be performed with the Filter parameter. By default, FindPackage returns all packages that starts with the
        /// Filter value.
        /// </summary>
        [Parameter]
        public SwitchParameter ExactMatch { get; set; }

        protected override ILogger Logger
        {
            get
            {
                // We don't want this cmdlet to print anything
                return NullLogger.Instance;
            }
        }

        protected override void ProcessRecordCore()
        {
            // Since this is used for intellisense, we need to limit the number of packages that we return. Otherwise,
            // typing InstallPackage TAB would download the entire feed.
            First = MaxReturnedPackages;
            base.ProcessRecordCore();
        }

        protected override IQueryable<IPackage> GetPackages(IPackageRepository sourceRepository)
        {
            IQueryable<IPackage> packages;
            if (!String.IsNullOrEmpty(Filter))
            {
                if (ExactMatch)
                {
                    packages = sourceRepository.FindPackagesById(Filter).AsQueryable();
                }
                else
                {
                    packages = sourceRepository.GetPackages()
                                               .Where(p => p.Id.ToLower().StartsWith(Filter.ToLower()));
                }
            }
            else
            {
                packages = sourceRepository.GetPackages();
            }

            return packages.OrderByDescending(p => p.DownloadCount)
                           .ThenBy(p => p.Id)
                           .AsQueryable();
        }

        protected override IQueryable<IPackage> GetPackagesForUpdate(IPackageRepository sourceRepository)
        {
            IPackageRepository localRepository = PackageManager.LocalRepository;
            var packagesToUpdate = localRepository.GetPackages();

            if (!String.IsNullOrEmpty(Filter))
            {
                packagesToUpdate = packagesToUpdate.Where(p => p.Id.StartsWith(Filter, StringComparison.OrdinalIgnoreCase));
            }

            return sourceRepository.GetUpdates(packagesToUpdate, IncludePrerelease, includeAllVersions: AllVersions)
                                   .OrderByDescending(p => p.DownloadCount)
                                   .ThenBy(p => p.Id)
                                   .AsQueryable();
        }
    }
}