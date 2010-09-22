using System;
using System.Linq;
using System.Windows;

namespace NuPack.Dialog.Providers {

    internal class UpdatePackagesProvider : OnlinePackagesProvider {

        private ResourceDictionary _resources;

        public UpdatePackagesProvider(ResourceDictionary resources ) : base(resources, false) {
            _resources = resources;
        }

        public override string Name {
            get {
                // TODO: Localize this string
                return "Updates";
            }
        }

        private object _mediumIconDataTemplate;
        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["OnlineUpdateTileTemplate"];
                }
                return _mediumIconDataTemplate;
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