using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    internal class OnlineSearchProvider : PackagesProviderBase
    {
        private static readonly PackageSortDescriptor _defaultSortDescriptor = new PackageSortDescriptor(Resources.Dialog_SortOption_Relevance, "Relevance", ListSortDirection.Ascending);
        private readonly PackagesProviderBase _baseProvider;

        public OnlineSearchProvider(PackagesProviderBase baseProvider)
            : base(baseProvider)
        {
            _baseProvider = baseProvider;
        }

        public override string Name
        {
            get { return _baseProvider.Name; }
        }

        internal override bool IncludePrerelease
        {
            get
            {
                return _baseProvider.IncludePrerelease;
            }
            set
            {
                _baseProvider.IncludePrerelease = value;
            }
        }

        internal static PackageSortDescriptor RelevanceSortDescriptor
        {
            get { return _defaultSortDescriptor; }
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return _baseProvider.CreateExtension(package);
        }

        public override bool CanExecute(PackageItem item)
        {
            return _baseProvider.CanExecute(item);
        }

        protected override IList<IVsSortDescriptor> CreateSortDescriptors()
        {
            var sortDescriptors = base.CreateSortDescriptors().ToList();
            sortDescriptors.Insert(0, _defaultSortDescriptor);
            return sortDescriptors;
        }

        public override void OnPackageLoadCompleted(PackagesTreeNodeBase selectedNode)
        {
            _baseProvider.OnPackageLoadCompleted(selectedNode);
        }

        protected internal override void RemoveSearchNode()
        {
            _baseProvider.RemoveSearchNode();
        }
    }
}