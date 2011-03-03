using System;

namespace NuGet.VisualStudio {
    public sealed class PersistencePackageMetadata : IEquatable<PersistencePackageMetadata>, IComparable<PersistencePackageMetadata> {

        public PersistencePackageMetadata(IPackageMetadata package, DateTime lastUsedDate) : 
            this(package.Id, package.Version, lastUsedDate) {
        }

        public PersistencePackageMetadata(string id, string version, DateTime lastUsedDate) : 
            this(id, new Version(version), lastUsedDate){
        }

        internal PersistencePackageMetadata(string id, string version) :
            this(id, new Version(version), DateTime.MinValue) {
        }

        private PersistencePackageMetadata(string id, Version version, DateTime lastUsedDate) {
            Id = id;
            Version = version;
            LastUsedDate = lastUsedDate;
        }

        public string Id { get; private set; }
        public Version Version { get; private set; }
        public DateTime LastUsedDate { get; private set; }

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

        public int CompareTo(PersistencePackageMetadata other) {
            return other.LastUsedDate.CompareTo(LastUsedDate);
        }
    }
}