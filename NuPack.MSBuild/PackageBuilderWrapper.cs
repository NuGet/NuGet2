using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NuPack.Authoring {
    public class PackageBuilderWrapper : IPackageBuilder {
        readonly PackageBuilder _instance;

        public PackageBuilderWrapper(PackageBuilder instance) {
            _instance = instance;
        }
        
        public void Save(Stream stream, string basePath) {
            _instance.Save(stream, basePath: basePath);
        }

        public string Id {
            get {
                return _instance.Manifest.Metadata.Id;
            }
        }

        public string Version {
            get {
                return _instance.Manifest.Metadata.Version;
            }
        }
    }
}
