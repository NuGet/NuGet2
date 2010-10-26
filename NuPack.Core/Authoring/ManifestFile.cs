using System.Xml.Serialization;

namespace NuPack {
    [XmlType("file", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestFile {
        [XmlElement("src")]
        public string Source { get; set; }

        [XmlElement("target")]
        public string Target { get; set; }
    }
}
