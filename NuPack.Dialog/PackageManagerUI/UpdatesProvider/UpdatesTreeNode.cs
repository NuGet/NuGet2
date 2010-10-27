using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    /// <summary>
    /// This class represents a tree node under the Updates tab
    /// </summary>
    internal class UpdatesTreeNode : PackagesTreeNodeBase {
        private readonly string _category;
        private readonly IPackageRepository _localRepository;
        private readonly IPackageRepository _sourceRepository;

        public UpdatesTreeNode(
            PackagesProviderBase provider, 
            string category, 
            IVsExtensionsTreeNode parent, 
            IPackageRepository localRepository, 
            IPackageRepository sourceRepository) :
            base(parent, provider) {

            _category = category;
            _localRepository = localRepository;
            _sourceRepository = sourceRepository;
        }

        public override string Name {
            get {
                return _category;
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            return _localRepository.GetUpdates(_sourceRepository).AsQueryable();
        }
    }
}
