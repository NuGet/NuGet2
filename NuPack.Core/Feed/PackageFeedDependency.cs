namespace NuPack {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// This type is used to serialize a package dependency into the atom feed.
    /// </summary>
    [DataContract(Name = "dependency", Namespace = Package.SchemaNamespace)]
    public class PackageFeedDependency {
        // Need parameterless ctor for serializer
        public PackageFeedDependency() {
        }

        public PackageFeedDependency(PackageDependency dependency) {
            Id = dependency.Id;
            Version = GetVersionString(dependency.Version);
            MinVersion = GetVersionString(dependency.MinVersion);
            MaxVersion = GetVersionString(dependency.MaxVersion);
        }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }

        [DataMember(Name = "minVersion", EmitDefaultValue = false)]
        public string MinVersion { get; set; }

        [DataMember(Name = "maxVersion", EmitDefaultValue = false)]
        public string MaxVersion { get; set; }

        public PackageDependency ToPackageDependency() {
            return PackageDependency.CreateDependency(Id,
                                                      Utility.ParseOptionalVersion(MinVersion),
                                                      Utility.ParseOptionalVersion(MaxVersion),
                                                      Utility.ParseOptionalVersion(Version));
        }

        private static string GetVersionString(Version version) {
            return version == null ? null : version.ToString();
        }
    }
}