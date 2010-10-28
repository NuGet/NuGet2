using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet {
    [XmlType("metadata")]
    public class ManifestMetadata : IPackageMetadata, IValidatableObject {
        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlElement("id")]
        public string Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlElement("authors")]
        public string Authors { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }

        [XmlElement("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("summary")]
        public string Summary { get; set; }

        [XmlElement("language")]
        public string Language { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("dependencies")]
        [XmlArrayItem("dependency")]
        public List<ManifestDependency> Dependencies { get; set; }

        Version IPackageMetadata.Version {
            get {
                if (Version == null) {
                    return null;
                }
                return new Version(Version);
            }
        }

        Uri IPackageMetadata.IconUrl {
            get {
                if (IconUrl == null) {
                    return null;
                }
                return new Uri(IconUrl);
            }
        }

        Uri IPackageMetadata.LicenseUrl {
            get {
                if (LicenseUrl == null) {
                    return null;
                }
                return new Uri(LicenseUrl);
            }
        }

        Uri IPackageMetadata.ProjectUrl {
            get {
                if (ProjectUrl == null) {
                    return null;
                }
                return new Uri(ProjectUrl);
            }
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                if (String.IsNullOrEmpty(Authors)) {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                if (Dependencies == null) {
                    return Enumerable.Empty<PackageDependency>();
                }
                return from dependency in Dependencies
                       select PackageDependency.CreateDependency(dependency.Id,
                                                                 Utility.ParseOptionalVersion(dependency.MinVersion),
                                                                 Utility.ParseOptionalVersion(dependency.MaxVersion),
                                                                 Utility.ParseOptionalVersion(dependency.Version));
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (LicenseUrl == String.Empty) {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "LicenseUrl"));
            }

            if (IconUrl == String.Empty) {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "IconUrl"));
            }

            if (ProjectUrl == String.Empty) {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "ProjectUrl"));
            }

            if (RequireLicenseAcceptance && String.IsNullOrWhiteSpace(LicenseUrl)) {
                yield return new ValidationResult(NuGetResources.Manifest_RequireLicenseAcceptanceRequiresLicenseUrl);
            }
        }
    }
}