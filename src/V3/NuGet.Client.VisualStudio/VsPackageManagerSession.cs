using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    public abstract class VsPackageManagerSession : PackageManagerSession
    {
        private IVsPackageSourceProvider _packageSourceProvider;
        private IPackageRepositoryFactory _repoFactory;

        protected ILogger Logger { get; private set; }

        public override NuGet.Client.PackageSource ActiveSource
        {
            get { return GetActiveSource(); }
        }

        protected VsPackageManagerSession(ILogger logger) : this(
            ServiceLocator.GetInstance<IVsPackageSourceProvider>(), 
            ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
            logger)
        {
        }

        protected VsPackageManagerSession(IVsPackageSourceProvider packageSourceProvider, IPackageRepositoryFactory repoFactory, ILogger logger)
        {
            _packageSourceProvider = packageSourceProvider;
            _repoFactory = repoFactory;
            Logger = logger;
        }

        public static VsPackageManagerSession ForProject(EnvDTE.Project project)
        {
            var packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            return new ProjectPackageManagerSession(
                project,
                packageManagerFactory.CreatePackageManagerToManageInstalledPackages().GetProjectManager(project));
        }

        public override IEnumerable<NuGet.Client.PackageSource> GetAvailableSources()
        {
            return _packageSourceProvider.GetEnabledPackageSources()
                .Select(s => new NuGet.Client.PackageSource(s.Name, s.Source));
        }

        public override IPackageSearcher CreateSearcher()
        {
            return new V2InteropSearcher(GetActiveRepo());
        }

        protected virtual IPackageRepository GetActiveRepo()
        {
            //if (ActiveSource.IsAggregate())
            //{
            //    throw new InvalidOperationException(Strings.VsPackageManagerSession_CannotUseAggregateSource);
            //}

            var repo = _repoFactory.CreateRepository(ActiveSource.Url);
            return repo;
        }

        public override void ChangeActiveSource(string newSourceName)
        {
            var source = _packageSourceProvider.GetEnabledPackageSources().FirstOrDefault(s =>
                String.Equals(newSourceName, s.Name, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Strings.VsPackageManagerSession_UnknownSource,
                        newSourceName), 
                    "newSourceName");
            }
            _packageSourceProvider.ActivePackageSource = source;
        }

        private NuGet.Client.PackageSource GetActiveSource()
        {
            var source = GetV2ActiveSource();
            return new NuGet.Client.PackageSource(source.Name, source.Source);
        }

        private NuGet.PackageSource GetV2ActiveSource()
        {
            var trueActive = _packageSourceProvider.ActivePackageSource;
            if (trueActive == null || trueActive.IsAggregate())
            {
                var firstAvailable = _packageSourceProvider.GetEnabledPackageSources().FirstOrDefault();
                Logger.Log(MessageLevel.Debug, "Current repo is Aggregate, replacing with '{0}'", firstAvailable.Name);
                return firstAvailable;
            }
            return trueActive;
        }
    }
}
