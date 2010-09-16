using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuPack.VisualStudio {
    public class VSPackageSourceProvider {
        
        private PackageSourceSettingsManager _settingsManager;
        private HashSet<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        private static readonly ConcurrentDictionary<_DTE, VSPackageSourceProvider> _cache = new ConcurrentDictionary<_DTE, VSPackageSourceProvider>();

        private VSPackageSourceProvider(IServiceProvider serviceProvider) {
            _settingsManager = new PackageSourceSettingsManager(serviceProvider);

            DeserializePackageSources();
            DeserializeActivePackageSource();
        }

        public static VSPackageSourceProvider Create(_DTE dte) {
            return _cache.GetOrAdd(
                dte, 
                x => new VSPackageSourceProvider(new ServiceProvider(x as Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
            );
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
                if (source.Equals(ActivePackageSource)) {
                    ActivePackageSource = null;
                }
            }

            return result;
        }

        private void PersistPackageSources() {
            _settingsManager.PackageSourcesString = SerializationHelper.Serialize(_packageSources);
        }
    }
}