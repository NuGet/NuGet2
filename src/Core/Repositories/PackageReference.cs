using System;

namespace NuGet {
    public class PackageReference {
        public PackageReference(string id, Version version) {
            Id = id;
            Version = version;
        }

        public string Id { get; private set; }
        public Version Version { get; private set; }
    }
}
