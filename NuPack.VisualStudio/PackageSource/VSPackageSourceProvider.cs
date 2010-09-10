using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuPack.VisualStudio {
    public class VSPackageSourceProvider : PackageSourceProvider {
        
        private PackageSourceSettingsManager _settingsManager;
        private HashSet<PackageSource> _packageSources;

        public VSPackageSourceProvider(_DTE dte) {

            ServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            _settingsManager = new PackageSourceSettingsManager(serviceProvider);
            
            DeserializePackageSources();
            DeserializeActivePackageSource();
        }

        private void DeserializePackageSources() {
            string propertyString = _settingsManager.PackageSourcesString;

            if (string.IsNullOrEmpty(propertyString)) {
                return;
            }

            _packageSources = SerializationHelper.Deserialize<HashSet<PackageSource>>(propertyString);
        }

        private void DeserializeActivePackageSource() {
            base.ActivePackageSource = SerializationHelper.Deserialize<PackageSource>(_settingsManager.ActivePackageSourceString);
        }
        
        public override PackageSource ActivePackageSource {
            get {
                return base.ActivePackageSource;
            }
            set {
                base.ActivePackageSource = value;

                // persist the value into VS settings store
                _settingsManager.ActivePackageSourceString = SerializationHelper.Serialize(value);
            }
        }

        public override IEnumerable<PackageSource> GetPackageSources() {
            return _packageSources;
        }

        public override void AddPackageSource(PackageSource source) {

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (!_packageSources.Contains(source)) {
                _packageSources.Add(source);
                PersistPackageSources();
            }
        }

        public override bool RemovePackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            bool result = _packageSources.Remove(source);
            if (result) {
                PersistPackageSources();
            }

            return result;
        }       

        private void PersistPackageSources() {
            _settingsManager.PackageSourcesString = SerializationHelper.Serialize(_packageSources);
        }
    }
}