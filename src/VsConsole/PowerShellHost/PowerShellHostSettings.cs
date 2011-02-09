using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NuGet;
using NuGet.VisualStudio;

namespace NuGetConsole.Host.PowerShell {

    internal class PowerShellHostSettings : IHostSettings {

        private readonly ISolutionManager _solutionManager;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ObservableCollection<string> _packageSources = new ObservableCollection<string>();
        private readonly ObservableCollection<string> _allProjects = new ObservableCollection<string>();

        public PowerShellHostSettings(ISolutionManager solutionManager, IPackageSourceProvider packageSourceProvider) {

            _packageSourceProvider = packageSourceProvider;
            _packageSourceProvider.PackageSourcesChanged += OnPackageSourcesChanged;
            OnPackageSourcesChanged(this, EventArgs.Empty);

            _solutionManager = solutionManager;
            _solutionManager.ProjectCollectionChanged += OnProjectCollectionChanged;
            OnProjectCollectionChanged(this, EventArgs.Empty);
        }

        private void OnPackageSourcesChanged(object sender, EventArgs e) {
            // reset the whole project collections.
            // it's not optimal, but the number of package sources is small anyway.
            _packageSources.Clear();
            _packageSources.AddRange(_packageSourceProvider.GetPackageSources().Select(p => p.Name));

            // when the whole collection changes, tell the toolbar to udpate the selected package source too
            RaisePropertyChangedEvent("ActivePackageSource");
        }

        private void OnProjectCollectionChanged(object sender, EventArgs e) {
            // reset the whole project collections.
            // it's not optimal, but the number of projects in the solution is small anyway.
            _allProjects.Clear();
            _allProjects.AddRange(_solutionManager.GetProjects().Select(p => p.Name));

            // when the whole collection changes, tell the toolbar to update the default project too
            RaisePropertyChangedEvent("DefaultProject");
        }

        public string ActivePackageSource {
            get {
                var activePackageSource = _packageSourceProvider.ActivePackageSource;
                return activePackageSource == null ? null : activePackageSource.Name;
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    throw new ArgumentNullException("value");
                }

                if (_packageSourceProvider.ActivePackageSource == null || 
                    _packageSourceProvider.ActivePackageSource.Name != value) {

                    _packageSourceProvider.ActivePackageSource =
                        _packageSourceProvider.GetPackageSources().FirstOrDefault(
                            ps => ps.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        public ObservableCollection<string> PackageSources {
            get {
                return _packageSources;
            }
        }

        public string DefaultProject {
            get {
                return _solutionManager.DefaultProjectName;
            }
            set {
                if (_solutionManager.DefaultProjectName != value) {
                    _solutionManager.DefaultProjectName = value;
                }
            }
        }

        public ObservableCollection<string> AvailableProjects {
            get {
                return _allProjects;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}