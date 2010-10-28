using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace NuGet {
    [XmlType("package")]
    public class Manifest {
        public Manifest() {
            Metadata = new ManifestMetadata();
        }

        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("files")]
        [XmlArrayItem("file", IsNullable = false)]
        public List<ManifestFile> Files { get; set; }

        public void Save(Stream stream) {
            // Validate before saving
            Validate(this);

            var serializer = new XmlSerializer(typeof(Manifest));
            serializer.Serialize(stream, this);
        }

        public static Manifest ReadFrom(Stream stream) {
            // Read the document
            XDocument document = XDocument.Load(stream);

            // Remove the schema namespace
            foreach (var e in document.Descendants()) {
                e.Name = e.Name.LocalName;
            }

            var serializer = new XmlSerializer(typeof(Manifest));
            var manifest = (Manifest)serializer.Deserialize(document.CreateReader());

            // Validate before returning
            Validate(manifest);

            return manifest;
        }

        public static Manifest Create(IPackageMetadata metadata) {
            return new Manifest {
                Metadata = new ManifestMetadata {
                    Id = metadata.Id,
                    Version = GetVersionString(metadata.Version),
                    Title = metadata.Title,
                    Authors = metadata.Authors == null ||
                              !metadata.Authors.Any() ? null : String.Join(",", metadata.Authors),
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

        private static void Validate(Manifest manifest) {
            var results = new List<ValidationResult>();
            TryValidate(manifest.Metadata, results);
            TryValidate(manifest.Files, results);
            TryValidate(manifest.Metadata.Dependencies, results);

            if (results.Any()) {
                string message = String.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                throw new ValidationException(message);
            }
        }

        private static bool TryValidate(object value, ICollection<ValidationResult> results) {
            if (value != null) {
                var enumerable = value as IEnumerable;
                if (enumerable != null) {
                    foreach (var item in enumerable) {
                        Validator.TryValidateObject(item, CreateValidationContext(item), results);
                    }
                }
                return Validator.TryValidateObject(value, CreateValidationContext(value), results);
            }
            return true;
        }

        private static ValidationContext CreateValidationContext(object value) {
            return new ValidationContext(value, NullServiceProvider.Instance, new Dictionary<object, object>());
        }

        private static string GetVersionString(Version version) {
            if (version == null) {
                return null;
            }
            return version.ToString();
        }

        private class NullServiceProvider : IServiceProvider {
            private static readonly IServiceProvider _instance = new NullServiceProvider();

            public static IServiceProvider Instance {
                get {
                    return _instance;
                }
            }

            public object GetService(Type serviceType) {
                return null;
            }
        }
    }
}