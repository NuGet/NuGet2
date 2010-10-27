using System.Xml.Serialization;

namespace NuGet {
    [XmlType("file", Namespace = Constants.ManifestSchemaNamespace)]
    public class ManifestFile {
        [XmlAttribute("src")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }
    }
}
