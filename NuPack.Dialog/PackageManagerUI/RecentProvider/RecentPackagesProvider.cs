using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

        /// <summary>
        /// Returns a fake list of packages for now
        /// </summary>
        /// <returns></returns>
        public override IQueryable<IPackage> GetQuery() {            
            var recent = new List<string>() { "elmah", "Antlr" };

            return PackagesRepository.GetPackages().Join(recent, p => p.Id, r => r, (p, r) => p).Distinct();
        }

    }
}
