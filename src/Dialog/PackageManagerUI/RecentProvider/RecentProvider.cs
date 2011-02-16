using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class RecentProvider : OnlineProvider {

        private readonly IPackageRepository _recentPackagesRepository;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private IVsPackageManager _recentPackageManager;

        public RecentProvider(
            Project project,
            IProjectManager projectManager,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IVsPackageManagerFactory packageManagerFactory,
            IPackageRepository recentPackagesRepository,
            ProviderServices providerServices)
            : base(project, projectManager, resources, packageRepositoryFactory, null, packageManagerFactory, providerServices) {

            _recentPackagesRepository = recentPackagesRepository;
            _packageManagerFactory = packageManagerFactory;
            _packageRepositoryFactory = packageRepositoryFactory;
        }

        public override string Name {
            get {
                return Resources.Dialog_RecentProvider;
            }
        }

        public override float SortOrder {
            get {
                return 4.0f;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                return true;
            }
        }

        protected internal override IVsPackageManager GetActivePackageManager() {
            if (_recentPackageManager == null) {
                var packageSource = new PackageSource("All") { IsAggregate = true };
                var repository = _packageRepositoryFactory.CreateRepository(packageSource);

                _recentPackageManager = _packageManagerFactory.CreatePackageManager(repository);
            }

            return _recentPackageManager;
        }

        protected override IList<IVsSortDescriptor> CreateSortDescriptors() {
            return new List<IVsSortDescriptor> {
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), "Id"),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), "Id", ListSortDirection.Descending)
                  };
        }

        protected override void FillRootNodes() {
            var allNode = new SimpleTreeNode(this, Resources.Dialog_RootNodeAll, RootNode, _recentPackagesRepository);
            RootNode.Nodes.Add(allNode);
        }

        public override string NoItemsMessage {
            get {
                return Resources.Dialog_RecentProviderNoItem;
            }
        }
    }
}