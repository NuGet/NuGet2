using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace NuPack {
    [XmlType("package", Namespace = Constants.ManifestSchemaNamespace)]
    public class Manifest {
        public Manifest() {
            Metadata = new ManifestMetadata();
        }

        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<ManifestFile> Files { get; set; }

        public void Save(Stream stream) {
            var serializer = new XmlSerializer(typeof(Manifest));
            serializer.Serialize(stream, this);
        }
    }
}
