using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuPack.VisualStudio {
    public class VSPackageSourceProvider {
        
        private PackageSourceSettingsManager _settingsManager;
        private HashSet<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        public VSPackageSourceProvider(IServiceProvider serviceProvider) {
            _settingsManager = new PackageSourceSettingsManager(serviceProvider);

            DeserializePackageSources();
            DeserializeActivePackageSource();
        }

        public VSPackageSourceProvider(_DTE dte) : 
            this(new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
        {
        }

        private void DeserializePackageSources() {
            string propertyString = _settingsManager.PackageSourcesString;

            if (!String.IsNullOrEmpty(propertyString)) {
                _packageSources = SerializationHelper.Deserialize<HashSet<PackageSource>>(propertyString);
            }
            
            if (_packageSources == null) {
                _packageSources = new HashSet<PackageSource>();
            }
        }

        private void DeserializeActivePackageSource() {
            ActivePackageSource = SerializationHelper.Deserialize<PackageSource>(_settingsManager.ActivePackageSourceString);
        }
        
        public PackageSource ActivePackageSource {
            get {
                return _activePackageSource;
            }
            set {
                _activePackageSource = value;

                // persist the value into VS settings store
                _settingsManager.ActivePackageSourceString = SerializationHelper.Serialize(value);
            }
        }

        public IEnumerable<PackageSource> GetPackageSources() {
            return _packageSources;
        }

        public void AddPackageSource(PackageSource source) {

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (!_packageSources.Contains(source)) {
                _packageSources.Add(source);
                PersistPackageSources();
            }
        }

        public bool RemovePackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            bool result = _packageSources.Remove(source);
            if (result) {
                PersistPackageSources();
            }

            return result;
        }

        public void TryAddAndSetActivePackageSource(string name, string source) {
            PackageSource packageSource = new PackageSource(name, source);
            if (!_packageSources.Contains(packageSource)) {
                _packageSources.Add(packageSource);
            }

            ActivePackageSource = packageSource;
        }

        private void PersistPackageSources() {
            _settingsManager.PackageSourcesString = SerializationHelper.Serialize(_packageSources);
        }
    }
}