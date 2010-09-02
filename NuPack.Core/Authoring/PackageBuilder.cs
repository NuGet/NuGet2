using System;
using System.Collections.Generic;
using System.IO;
using NuPack.Resources;

namespace NuPack {
    public partial class PackageBuilder{
        private readonly Dictionary<PackageFileType, List<IPackageFile>> _packageFiles;
        private readonly List<IPackageAssemblyReference> _references;
        private readonly List<PackageDependency> _dependencies;

        public PackageBuilder() {
            _packageFiles = new Dictionary<PackageFileType, List<IPackageFile>>();
            _dependencies = new List<PackageDependency>();
            _references = new List<IPackageAssemblyReference>();
            Keywords = new List<string>();
            Authors = new List<string>();
            
            foreach (int value in Enum.GetValues(typeof(PackageFileType))) {
                var key = (PackageFileType)value;
                _packageFiles[key] = new List<IPackageFile>();
            }
        }

        public string Id {
            get; set;
        }

        public string Description {
            get; set;
        }

        public List<string> Authors {
            get;
            private set;
        }

        public List<string> Keywords {
            get;
            private set;
        }

        public string Category {
            get; set;
        }

        public Version Version {
            get; set;
        }

        public DateTime Created {
            get; set;
        }

        public string Language {
            get; set;
        }

        public DateTime Modified {
            get; set;
        }

        public string LastModifiedBy {
            get; set;
        }

        public List<IPackageFile> PackageFiles {
            get {
                return _packageFiles[PackageFileType.Content];
            }
        }

        public List<IPackageFile> Resources {
            get {
                return _packageFiles[PackageFileType.Resource];
            }
        }
        
        public List<PackageDependency> Dependencies {
            get {
                return _dependencies;
            }
        }

        public List<IPackageAssemblyReference> References {
            get {
                return _references;
            }
        }

        public List<IPackageFile> GetFiles(PackageFileType type) {
            return _packageFiles[type];
        }

        public void Save(Stream stream) {
            if (!IsValidBuild()) {
                throw new InvalidOperationException(NuPackResources.PackageBuilder_IdAndVersionRequired);
            }
           WritePackageContent(stream);
        }

        private bool IsValidBuild() {
            return !String.IsNullOrEmpty(Id) && (Version != null);
        }
    }
}
