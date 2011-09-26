using System;

namespace NuGet {
    public class PackageReference {
        public PackageReference(string id, SemVer version, IVersionSpec versionConstraint) {
            Id = id;
            Version = version;
            VersionConstraint = versionConstraint;
        }

        public string Id { get; private set; }
        public SemVer Version { get; private set; }
        public IVersionSpec VersionConstraint { get; set; }

        public override bool Equals(object obj) {
            var reference = obj as PackageReference;
            if (reference != null) {
                return Id.Equals(reference.Id, StringComparison.OrdinalIgnoreCase) &&
                       Version.Equals(reference.Version);
            }

            return false;
        }

        public override int GetHashCode() {
            var combiner = new HashCodeCombiner();
            combiner.AddObject(Id);
            combiner.AddObject(Version);
            return combiner.CombinedHash;
        }

        public override string ToString() {
            if (VersionConstraint == null) {
                return Id + " " + Version;
            }
            return Id + " " + Version + " (" + VersionConstraint + ")";
        }
    }
}
