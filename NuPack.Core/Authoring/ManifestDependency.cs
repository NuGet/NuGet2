using System.Xml.Serialization;

namespace NuPack {
    [XmlType("dependency", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestDependency {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("minVersion")]
        public string MinVersion { get; set; }

        [XmlAttribute("maxVersion")]
        public string MaxVersion { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }
    }
}
