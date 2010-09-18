using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NuPack.Dialog.ToolsOptionsUI;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages that were recently used.
    /// </summary>
    internal class RecentPackagesProvider : OnlinePackagesProvider {
        public RecentPackagesProvider(ResourceDictionary resources)
            : base(resources, false) {
        }

        public override string Name {
            get {
                return "Recent";
            }
        }

        protected override IPackageRepository PackagesRepository {
            get {
                if (_packagesRepository == null) {
                    _feed = Settings.RepositoryServiceUri;

                    if (String.IsNullOrEmpty(_feed)) {
                        return EmptyPackageRepository.Default;
                    }

                    _dte = Utilities.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    _project = GetActiveProject(_dte);

                    _vsPackageManager = VSPackageManager.GetPackageManager(_feed, _dte);
                    _vsProjectManager = _vsPackageManager.GetProjectManager(_project);

                    _packagesRepository = _vsPackageManager.ExternalRepository;
                }
                return _packagesRepository;
            }
        }

        /// <summary>
        /// Returns a fake list of packages for now
        /// </summary>
        /// <returns></returns>
        public override IQueryable<IPackage> GetQuery() {
            if (String.IsNullOrEmpty(_feed)) {
                return Enumerable.Empty<IPackage>().AsQueryable();
            }
            List<string> recent = new List<String>() { "elmah", "Antlr" };

            return _packagesRepository.GetPackages().Join(recent, p => p.Id, r => r, (p, r) => p).Distinct();
        }

    }
}
