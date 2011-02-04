using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using NuGet.Resources;
using NuGet;

namespace NuGet {
    [XmlType("package", Namespace = Constants.ManifestSchemaNamespace)]
    public class Manifest {
        private const string SchemaResourceName = "NuGet.Authoring.nuspec.xsd";

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

            // Add the schema namespace if it isn't there
            foreach (var e in document.Descendants()) {
                if (e.Name.Namespace == null || String.IsNullOrEmpty(e.Name.Namespace.NamespaceName)) {
                    e.Name = XName.Get(e.Name.LocalName, Constants.ManifestSchemaNamespace);
                }
            }

            // Validate the schema
            ValidateManifestSchema(document);

            // Remove the namespace from the outer tag to match CTP2 expectations
            document.Root.Name = document.Root.Name.LocalName;

            var serializer = new XmlSerializer(typeof(Manifest));
            var manifest = (Manifest)serializer.Deserialize(document.CreateReader());

            // Validate before returning
            Validate(manifest);

            // Trim fields in case they have extra whitespace
            manifest.Metadata.Id = manifest.Metadata.Id.SafeTrim();
            manifest.Metadata.Title = manifest.Metadata.Title.SafeTrim();
            manifest.Metadata.Authors = manifest.Metadata.Authors.SafeTrim();
            manifest.Metadata.Owners = manifest.Metadata.Owners.SafeTrim();
            manifest.Metadata.Description = manifest.Metadata.Description.SafeTrim();
            manifest.Metadata.Summary = manifest.Metadata.Summary.SafeTrim();
            manifest.Metadata.Language = manifest.Metadata.Language.SafeTrim();
            manifest.Metadata.Tags = manifest.Metadata.Tags.SafeTrim();

            return manifest;
        }

        public static Manifest Create(IPackageMetadata metadata) {
            return new Manifest {
                Metadata = new ManifestMetadata {
                    Id = metadata.Id.SafeTrim(),
                    Version = metadata.Version.ToStringSafe(),
                    Title = metadata.Title.SafeTrim(),
                    Authors = GetCommaSeparatedString(metadata.Authors),
                    Owners = GetCommaSeparatedString(metadata.Owners) ?? GetCommaSeparatedString(metadata.Authors),
                    Tags = String.IsNullOrEmpty(metadata.Tags) ? null : metadata.Tags.SafeTrim(),
                    LicenseUrl = metadata.LicenseUrl != null ? metadata.LicenseUrl.OriginalString.SafeTrim() : null,
                    ProjectUrl = metadata.ProjectUrl != null ? metadata.ProjectUrl.OriginalString.SafeTrim() : null,
                    IconUrl = metadata.IconUrl != null ? metadata.IconUrl.OriginalString.SafeTrim() : null,
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
                    Description = metadata.Description.SafeTrim(),
                    Summary = metadata.Summary.SafeTrim(),
                    Language = metadata.Language.SafeTrim(),
                    Dependencies = metadata.Dependencies == null ||
                                   !metadata.Dependencies.Any() ? null :
                                   (from d in metadata.Dependencies
                                    select new ManifestDependency {
                                        Id = d.Id.SafeTrim(),
                                        Version = d.VersionSpec.ToStringSafe()
                                    }).ToList()
                }
            };
        }

        private static string GetCommaSeparatedString(IEnumerable<string> values) {
            if (values == null || !values.Any()) {
                return null;
            }
            return String.Join(",", values);
        }

        private static void ValidateManifestSchema(XDocument document) {
            CheckSchemaVersion(document);

            // Create the schema set
            var schemaSet = new XmlSchemaSet();
            using (Stream schemaStream = GetSchemaStream()) {
                schemaSet.Add(Constants.ManifestSchemaNamespace, XmlReader.Create(schemaStream));
            }

            // Validate the document
            document.Validate(schemaSet, (sender, e) => {
                if (e.Severity == XmlSeverityType.Error) {
                    // Throw an exception if there is a validation error
                    throw new InvalidOperationException(e.Message);
                }
            });
        }

        private static void CheckSchemaVersion(XDocument document) {
            // Get the metadata node and look for the schemaVersion attribute
            XElement metadata = document.Root.Element(XName.Get("metadata", Constants.ManifestSchemaNamespace));
            if (metadata != null) {
                string schemaVersionString = metadata.GetOptionalAttributeValue("schemaVersion");

                // If there is any schemaVersion attribute then fail
                if (!String.IsNullOrEmpty(schemaVersionString)) {
                    // Get the package id for error reporting.
                    string packageId = metadata.GetOptionalElementValue("id", Constants.ManifestSchemaNamespace);

                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                                      NuGetResources.IncompatibleSchema,
                                      packageId,
                                      typeof(Manifest).Assembly.GetNameSafe().Version));
                }
            }
        }

        private static Stream GetSchemaStream() {
            return typeof(Manifest).Assembly.GetManifestResourceStream(SchemaResourceName);
        }

        private static void Validate(Manifest manifest) {
            var results = new List<ValidationResult>();

            // Run all data annotations validations
            TryValidate(manifest.Metadata, results);
            TryValidate(manifest.Files, results);
            TryValidate(manifest.Metadata.Dependencies, results);

            if (results.Any()) {
                string message = String.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                throw new ValidationException(message);
            }

            // Validate additonal dependency rules dependencies
            ValidateDependencies(manifest.Metadata);
        }

        private static void ValidateDependencies(IPackageMetadata metadata) {
            var dependencySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in metadata.Dependencies) {
                // Throw an error if this dependency has been defined more than once
                if (!dependencySet.Add(dependency.Id)) {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DuplicateDependenciesDefined, metadata.Id, dependency.Id));
                }

                // Validate the dependency version
                ValidateDependencyVersion(dependency);
            }
        }
        private static void ValidateDependencyVersion(PackageDependency dependency) {
            if (dependency.VersionSpec != null) {
                if (dependency.VersionSpec.MinVersion != null &&
                    dependency.VersionSpec.MaxVersion != null) {

                    if ((!dependency.VersionSpec.IsMaxInclusive ||
                         !dependency.VersionSpec.IsMinInclusive) &&
                        dependency.VersionSpec.MaxVersion == dependency.VersionSpec.MinVersion) {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }

                    if (dependency.VersionSpec.MaxVersion < dependency.VersionSpec.MinVersion) {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }
                }
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