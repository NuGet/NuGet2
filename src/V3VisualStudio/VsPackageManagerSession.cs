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
        private ILogger _logger;

        protected ILogger Logger { get { return _logger; } }

        public override PackageSource ActiveSource
        {
            get { return GetActiveSource(); }
        }

        protected VsPackageManagerSession() : this(
            ServiceLocator.GetInstance<IVsPackageSourceProvider>(), 
            ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
            ServiceLocator.GetInstance<ILogger>())
        {

        }

        protected VsPackageManagerSession(IVsPackageSourceProvider packageSourceProvider, IPackageRepositoryFactory repoFactory, ILogger logger)
        {
            _packageSourceProvider = packageSourceProvider;
            _repoFactory = repoFactory;
            _logger = logger;
        }

        public static VsPackageManagerSession ForProject(EnvDTE.Project project)
        {
            var packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            return new ProjectPackageManagerSession(
                project,
                packageManagerFactory.CreatePackageManagerToManageInstalledPackages().GetProjectManager(project));
        }

        public override IEnumerable<PackageSource> GetAvailableSources()
        {
            return _packageSourceProvider.GetEnabledPackageSources();
        }

        public override IPackageSearcher CreateSearcher()
        {
            if (ActiveSource.IsAggregate())
            {
                throw new InvalidOperationException(Strings.VsPackageManagerSession_CannotUseAggregateSource);
            }

            var v2Repo = _repoFactory.CreateRepository(ActiveSource.Source);
            return new V2InteropSearcher(v2Repo);
        }

        public override void ChangeActiveSource(string newSourceName)
        {
            var source = GetAvailableSources().FirstOrDefault(s =>
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

        private PackageSource GetActiveSource()
        {
            var trueActive = _packageSourceProvider.ActivePackageSource;
            if (trueActive == null || trueActive.IsAggregate())
            {
                var firstAvailable = GetAvailableSources().FirstOrDefault();
                _logger.Log(MessageLevel.Debug, "Current repo is Aggregate, replacing with '{0}'", firstAvailable.Name);
                return firstAvailable;
            }
            return trueActive;
        }
    }
}
