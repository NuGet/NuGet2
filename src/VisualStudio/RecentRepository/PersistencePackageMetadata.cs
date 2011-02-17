using System;

namespace NuGet.VisualStudio {
    public sealed class PersistencePackageMetadata : IEquatable<PersistencePackageMetadata> {

        public PersistencePackageMetadata(IPackageMetadata package) {
            Id = package.Id;
            Version = package.Version;
        }

        public PersistencePackageMetadata(string id, string version) {
            Id = id;
            Version = new Version(version);
        }

        public string Id { get; private set; }
        public Version Version { get; private set; }

        public bool Equals(IPackageMetadata metadata) {
            return Equals(metadata.Id, metadata.Version);
        }

        public bool Equals(PersistencePackageMetadata other) {
            return Equals(other.Id, other.Version);
        }

        private bool Equals(string id, Version version) {
            return Id.Equals(id, StringComparison.OrdinalIgnoreCase) && Version == version;
        }

        public override int GetHashCode() {
            return Id.GetHashCode() * 3137 + Version.GetHashCode();
        }
    }
}