using System;
using System.Linq;
using System.Windows;

namespace NuPack.Dialog.Providers {

    internal class UpdatePackagesProvider : OnlinePackagesProvider {

        public UpdatePackagesProvider(ResourceDictionary resources ) : base(resources, false) {
        }

        public override string Name {
            get {
                // TODO: Localize this string
                return "Updates";
            }
        }

        public override IQueryable<IPackage> GetQuery() {
            var localPackages = PackageManager.SolutionRepository.GetPackages();

            return from p in PackageManager.SourceRepository.GetPackages()
                   where localPackages.Any(q => p.Id.Equals(q.Id, StringComparison.OrdinalIgnoreCase) && q.Version < p.Version)
                   select p;
        }
    }
}