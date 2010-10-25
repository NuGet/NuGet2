using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

        public static Manifest Create(IPackageMetadata metadata) {
            return new Manifest {
                Metadata = new ManifestMetadata {
                    Id = metadata.Id,
                    Version = GetVersionString(metadata.Version),
                    Title = metadata.Title,
                    Authors = String.Join(",", metadata.Authors),
                    LicenseUrl = metadata.LicenseUrl != null ? metadata.LicenseUrl.OriginalString : null,
                    ProjectUrl = metadata.ProjectUrl != null ? metadata.ProjectUrl.OriginalString : null,
                    IconUrl = metadata.IconUrl != null ? metadata.IconUrl.OriginalString : null,
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
                    Description = metadata.Description,
                    Summary = metadata.Summary,
                    Dependencies = metadata.Dependencies == null ||
                                   !metadata.Dependencies.Any() ? null :
                                   (from d in metadata.Dependencies
                                    select new ManifestDependency {
                                        Id = d.Id,
                                        MinVersion = GetVersionString(d.MinVersion),
                                        MaxVersion = GetVersionString(d.MaxVersion),
                                        Version = GetVersionString(d.Version)
                                    }).ToList()
                }
            };
        }

        private static string GetVersionString(Version version) {
            if (version == null) {
                return null;
            }
            return version.ToString();
        }
    }
}
