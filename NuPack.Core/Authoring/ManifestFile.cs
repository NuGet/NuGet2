using System.Xml.Serialization;

namespace NuPack {
    [XmlType("file", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestFile {
        [XmlAttribute("src")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }
    }
}
