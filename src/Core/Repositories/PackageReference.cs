using System;

namespace NuGet {
    public class PackageReference {
        public PackageReference(string id, Version version, IVersionSpec versionConstraint) {
            Id = id;
            Version = version;
            VersionConstraint = versionConstraint;
        }

        public string Id { get; private set; }
        public Version Version { get; private set; }
        public IVersionSpec VersionConstraint { get; set; }
    }
}
