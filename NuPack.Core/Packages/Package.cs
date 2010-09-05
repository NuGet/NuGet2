namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public abstract class Package {
        internal const string SchemaNamespace = "http://schemas.microsoft.com/packaging/2010/07/";
        internal const string ManifestSchemaNamespace = SchemaNamespace + "nuspec.xsd";

        public abstract string Id {
            get;
        }

        public abstract string Description {
            get;
        }

        public abstract IEnumerable<string> Authors {
            get;
        }

        public abstract IEnumerable<string> Keywords {
            get;
        }

        public abstract string Category {
            get;
        }

        public abstract Version Version {
            get;
        }

        public abstract DateTime Created {
            get;
        }

        public abstract string Language {
            get;
        }

        public abstract DateTime Modified {
            get;
        }

        public abstract string LastModifiedBy {
            get;
        }

        public bool HasProjectContent {
            get {
                return AssemblyReferences.Any() || this.GetContentFiles().Any();
            }
        }

        public abstract IEnumerable<IPackageFile> GetFiles();

        public abstract IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get;
        }

        public abstract IEnumerable<PackageDependency> Dependencies {
            get;
        }

        public override string ToString() {
            return Id + " " + Version;
        }

        public static Package Open(Stream stream) {
            return new ZipPackage(stream);
        }
    }
}
