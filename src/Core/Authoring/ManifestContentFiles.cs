using System.Xml.Serialization;

namespace NuGet
{
    [XmlType("files")]
    public class ManifestContentFiles
    {
        [XmlAttribute("include")]
        public string Include { get; set; }

        [XmlAttribute("buildAction")]
        public string BuildAction { get; set; }

        [XmlAttribute("copyToOutput")]
        public bool CopyToOutput { get; set; }

        [XmlAttribute("flatten")]
        public bool Flatten { get; set; }
    }
}