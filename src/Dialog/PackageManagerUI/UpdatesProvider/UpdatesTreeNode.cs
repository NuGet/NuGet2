using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    /// <summary>
    /// This class represents a tree node under the Updates tab
    /// </summary>
    internal class UpdatesTreeNode : SimpleTreeNode {
        private readonly IPackageRepository _localRepository;

        public UpdatesTreeNode(
            PackagesProviderBase provider, 
            string category, 
            IVsExtensionsTreeNode parent, 
            IPackageRepository localRepository, 
            IPackageRepository sourceRepository) :
            base(provider, category, parent, sourceRepository) {

            _localRepository = localRepository;
        }

        public override IQueryable<IPackage> GetPackages() {
            return Repository.GetUpdates(_localRepository.GetPackages()).AsQueryable();
        }
    }
}
