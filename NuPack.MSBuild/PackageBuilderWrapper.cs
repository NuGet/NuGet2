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

        public Collection<string> Authors {
            get { 
                return _instance.Authors; 
            }
        }

        public string Category {
            get {
                return _instance.Category;
            }
            set {
                _instance.Category = value;
            }
        }

        public DateTime Created {
            get {
                return _instance.Created;
            }
            set {
                _instance.Created = value;
            }
        }

        public Collection<PackageDependency> Dependencies {
            get {
                return _instance.Dependencies;
            }
        }

        public string Description {
            get {
                return _instance.Description;
            }
            set {
                _instance.Description = value;
            }
        }

        public Collection<IPackageFile> Files {
            get { 
                return _instance.Files; 
            }
        }

        public string Id {
            get {
                return _instance.Id;
            }
            set {
                _instance.Id = value;
            }
        }

        public Collection<string> Keywords {
            get {
                return _instance.Keywords;
            }
        }

        public string Language {
            get {
                return _instance.Language;
            }
            set {
                _instance.Language = value;
            }
        }

        public string LastModifiedBy {
            get {
                return _instance.LastModifiedBy;
            }
            set {
                _instance.LastModifiedBy = value;
            }
        }

        public Uri LicenseUrl {
            get {
                return _instance.LicenseUrl;
            }
            set {
                _instance.LicenseUrl = value;
            }
        }

        public DateTime Modified {
            get {
                return _instance.Modified;
            }
            set {
                _instance.Modified = value;
            }
        }

        public bool RequireLicenseAcceptance {
            get {
                return _instance.RequireLicenseAcceptance;
            }
            set {
                _instance.RequireLicenseAcceptance = value;
            }
        }

        public void Save(Stream stream) {
            _instance.Save(stream);
        }

        public Version Version {
            get {
                return _instance.Version;
            }
            set {
                _instance.Version = value;
            }
        }
    }
}
