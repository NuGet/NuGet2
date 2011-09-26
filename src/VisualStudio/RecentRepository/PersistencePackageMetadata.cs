using System;

namespace NuGet.VisualStudio {
    internal sealed class PersistencePackageMetadata : IPersistencePackageMetadata {

        public PersistencePackageMetadata(string id, string version, DateTime lastUsedDate) :
            this(id, new SemVer(version), lastUsedDate) {
        }

        public PersistencePackageMetadata(string id, string version) :
            this(id, new SemVer(version), DateTime.MinValue) {
        }

        public PersistencePackageMetadata(string id, SemVer version, DateTime lastUsedDate) {
            Id = id;
            Version = version;
            LastUsedDate = lastUsedDate;
        }

        public string Id { get; private set; }
        public SemVer Version { get; private set; }
        public DateTime LastUsedDate { get; private set; }
    }
}