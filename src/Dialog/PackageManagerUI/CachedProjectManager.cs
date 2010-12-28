using System;
using System.Collections.Generic;

namespace NuGet.Dialog.PackageManagerUI {
    internal class CachedProjectManager : IProjectManager {
        private readonly IProjectManager _projectManager;
        private readonly HashSet<IPackage> _cache;

        public CachedProjectManager(IProjectManager projectManager) {
            _projectManager = projectManager;
            
            _projectManager.PackageReferenceAdded += (sender, e) => {
                _cache.Add(e.Package);
            };

            _projectManager.PackageReferenceRemoved += (sender, e) => {
                _cache.Remove(e.Package);
            };

            _cache = new HashSet<IPackage>(projectManager.LocalRepository.GetPackages(), PackageEqualityComparer.IdAndVersion);
        }

        public IPackageRepository LocalRepository {
            get {
                return _projectManager.LocalRepository;
            }
        }

        public ILogger Logger {
            get {
                return _projectManager.Logger;
            }
            set {
                _projectManager.Logger = value;
            }
        }

        public IProjectSystem Project {
            get {
                return _projectManager.Project;
            }
        }

        public IPackageRepository SourceRepository {
            get {
                return _projectManager.SourceRepository;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded {
            add {
                _projectManager.PackageReferenceAdded += value;
            }
            remove {
                _projectManager.PackageReferenceAdded -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding {
            add {
                _projectManager.PackageReferenceAdding += value;
            }
            remove {
                _projectManager.PackageReferenceAdding -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved {
            add {
                _projectManager.PackageReferenceRemoved += value;
            }
            remove {
                _projectManager.PackageReferenceRemoved -= value;
            }
        }

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving {
            add {
                _projectManager.PackageReferenceRemoving += value;
            }
            remove {
                _projectManager.PackageReferenceRemoving -= value;
            }
        }

        public void AddPackageReference(string packageId, Version version, bool ignoreDependencies) {
            _projectManager.AddPackageReference(packageId, version, ignoreDependencies);
        }

        public void RemovePackageReference(string packageId, bool forceRemove, bool removeDependencies) {
            _projectManager.RemovePackageReference(packageId, forceRemove, removeDependencies);
        }

        public void UpdatePackageReference(string packageId, Version version, bool updateDependencies) {
            _projectManager.UpdatePackageReference(packageId, version, updateDependencies);
        }

        public bool IsInstalled(IPackage package) {
            return _cache.Contains(package);
        }
    }
}
