using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("metadata")]
    public class ManifestMetadata : IPackageMetadata, IValidatableObject
    {
        private string _owners;
        private string _minClientVersionString;

        [XmlAttribute("minClientVersion")]
        [ManifestVersion(5)]
        public string MinClientVersionString
        {
            get { return _minClientVersionString; }
            set
            {
                Version version = null;
                if (!String.IsNullOrEmpty(value) && !System.Version.TryParse(value, out version))
                {
                    throw new InvalidDataException(NuGetResources.Manifest_InvalidMinClientVersion);
                }

                _minClientVersionString = value;
                MinClientVersion = version;
            }
        }

        [XmlIgnore]
        public Version MinClientVersion { get; private set; }

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

        [XmlElement("owners")]
        public string Owners
        {
            get
            {
                // Fallback to authors
                return _owners ?? Authors;
            }
            set
            {
                _owners = value;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }

        [XmlElement("licenseNames")]
        public string LicenseNames { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("repositoryUrl")]
        public string RepositoryUrl { get; set; }

        [XmlElement("repositoryType")]
        public string RepositoryType { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Xml deserialziation can't handle uris")]
        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }

        [XmlElement("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [XmlElement("developmentDependency")]
        [System.ComponentModel.DefaultValue(false)]
        public bool DevelopmentDependency { get; set; }

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("summary")]
        public string Summary { get; set; }

        [XmlElement("releaseNotes")]
        [ManifestVersion(2)]
        public string ReleaseNotes { get; set; }

        [XmlElement("copyright")]
        [ManifestVersion(2)]
        public string Copyright { get; set; }

        [XmlElement("language")]
        public string Language { get; set; }

        [XmlElement("tags")]
        public string Tags { get; set; }

        /// <summary>
        /// This property should be used only by the XML serializer. Do not use it in code.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification="The propert setter is not supported.")]
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("dependencies", IsNullable = false)]
        [XmlArrayItem("group", typeof(ManifestDependencySet))]
        [XmlArrayItem("dependency", typeof(ManifestDependency))]
        public List<object> DependencySetsSerialize
        {
            get
            {
                if (DependencySets == null || DependencySets.Count == 0)
                {
                    return null;
                }

                if (DependencySets.Any(set => set.TargetFramework != null || (set.Properties != null && set.Properties.Any())))
                {
                    return DependencySets.Cast<object>().ToList();
                }
                else
                {
                    return DependencySets.SelectMany(set => set.Dependencies).Cast<object>().ToList();
                }
            }
            set
            {
                // this property is only used for serialization.
                throw new InvalidOperationException();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlIgnore]
        public List<ManifestDependencySet> DependencySets { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("frameworkAssemblies")]
        [XmlArrayItem("frameworkAssembly")]
        public List<ManifestFrameworkAssembly> FrameworkAssemblies { get; set; }

        /// <summary>
        /// This property should be used only by the XML serializer. Do not use it in code.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "The propert setter is not supported.")]
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("references", IsNullable = false)]
        [XmlArrayItem("group", typeof(ManifestReferenceSet))]
        [XmlArrayItem("reference", typeof(ManifestReference))]
        [ManifestVersion(2)]
        public List<object> ReferenceSetsSerialize
        {
            get
            {
                if (ReferenceSets == null || ReferenceSets.Count == 0)
                {
                    return null;
                }

                if (ReferenceSets.Any(set => set.TargetFramework != null || (set.Properties != null && set.Properties.Any())))
                {
                    return ReferenceSets.Cast<object>().ToList();
                }
                else
                {
                    return ReferenceSets.SelectMany(set => set.References).Cast<object>().ToList();
                }
            }
            set
            {
                // this property is only used for serialization.
                throw new InvalidOperationException();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlIgnore]
        public List<ManifestReferenceSet> ReferenceSets { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("properties")]
        [XmlArrayItem("property")]
        public List<ManifestProperty> Properties { get; set; }

        SemanticVersion IPackageName.Version
        {
            get
            {
                if (Version == null)
                {
                    return null;
                }
                return new SemanticVersion(Version);
            }
        }

        Uri IPackageMetadata.IconUrl
        {
            get
            {
                if (IconUrl == null)
                {
                    return null;
                }
                return new Uri(IconUrl);
            }
        }

        Uri IPackageMetadata.LicenseUrl
        {
            get
            {
                if (LicenseUrl == null)
                {
                    return null;
                }
                return new Uri(LicenseUrl);
            }
        }

        Uri IPackageMetadata.ProjectUrl
        {
            get
            {
                if (ProjectUrl == null)
                {
                    return null;
                }
                return new Uri(ProjectUrl);
            }
        }

        Uri IPackageMetadata.RepositoryUrl
        {
            get
            {
                if (RepositoryUrl == null)
                {
                    return null;
                }
                return new Uri(RepositoryUrl);
            }
        }

        IEnumerable<string> IPackageMetadata.Authors
        {
            get
            {
                if (String.IsNullOrEmpty(Authors))
                {
                    return Enumerable.Empty<string>();
                }
                return Authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<string> IPackageMetadata.Owners
        {
            get
            {
                if (String.IsNullOrEmpty(Owners))
                {
                    return Enumerable.Empty<string>();
                }
                return Owners.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        IEnumerable<PackageDependencySet> IPackageMetadata.DependencySets
        {
            get
            {
                if (DependencySets == null)
                {
                    return Enumerable.Empty<PackageDependencySet>();
                }
                
                var dependencySets = DependencySets.Select(CreatePackageDependencySet);

                // group the dependency sets with the same target framework and properties together.
                var groups = new Dictionary<Tuple<FrameworkName, string>, Tuple<IEnumerable<PackageProperty>, List<PackageDependency>>>();
                foreach (var set in dependencySets)
                {
                    var propertyGrouping = GetPropertyGrouping(set.Properties);

                    Tuple<IEnumerable<PackageProperty>, List<PackageDependency>> existing;
                    if (groups.TryGetValue(new Tuple<FrameworkName, string>(set.TargetFramework, propertyGrouping), out existing))
                    {
                        existing.Item2.AddRange(set.Dependencies);
                    }
                    else
                    {
                        groups.Add(new Tuple<FrameworkName, string>(set.TargetFramework, propertyGrouping),
                            new Tuple<IEnumerable<PackageProperty>, List<PackageDependency>>(set.Properties, set.Dependencies.ToList()));
                    }
                }

                return groups.Select(g => new PackageDependencySet(g.Key.Item1, g.Value.Item2, g.Value.Item1))
                    .OrderBy(g => g.TargetFramework.ToStringSafe())
                    .ThenBy(g => g.Properties.Count())
                    .ToList();
            }
        }

        ICollection<PackageReferenceSet> IPackageMetadata.PackageAssemblyReferences
        {
            get
            {
                if (ReferenceSets == null)
                {
                    return new PackageReferenceSet[0];
                }

                var referenceSets = ReferenceSets.Select(r => new PackageReferenceSet(r));

                // group the reference sets with the same target framework and properties together.
                var groups = new Dictionary<Tuple<FrameworkName, string>, Tuple<IEnumerable<PackageProperty>, List<string>>>();
                foreach (var set in referenceSets)
                {
                    var propertyGrouping = GetPropertyGrouping(set.Properties);

                    Tuple<IEnumerable<PackageProperty>, List<string>> existing;
                    if (groups.TryGetValue(new Tuple<FrameworkName, string>(set.TargetFramework, propertyGrouping), out existing))
                    {
                        existing.Item2.AddRange(set.References);
                    }
                    else
                    {
                        groups.Add(new Tuple<FrameworkName, string>(set.TargetFramework, propertyGrouping),
                            new Tuple<IEnumerable<PackageProperty>, List<string>>(set.Properties, set.References.ToList()));
                    }
                }

                return groups.Select(g => new PackageReferenceSet(g.Key.Item1, g.Value.Item2, g.Value.Item1))
                    .OrderBy(g => g.TargetFramework.ToStringSafe())
                    .ThenBy(g => g.Properties.Count())
                    .ToList();
            }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies
        {
            get
            {
                if (FrameworkAssemblies == null)
                {
                    return Enumerable.Empty<FrameworkAssemblyReference>();
                }

                return from frameworkReference in FrameworkAssemblies
                       select new FrameworkAssemblyReference(frameworkReference.AssemblyName, ParseFrameworkNames(frameworkReference.TargetFramework));
            }
        }

        IEnumerable<PackageProperty> IPackageMetadata.Properties
        {
            get
            {
                if (Properties == null)
                {
                    return Enumerable.Empty<PackageProperty>();
                }

                return from property in Properties
                       select new PackageProperty(property.Name, property.Value);
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!String.IsNullOrEmpty(Id))
            {
                if (Id.Length > PackageIdValidator.MaxPackageIdLength)
                {
                    yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_IdMaxLengthExceeded));
                }
                else if(!PackageIdValidator.IsValidPackageId(Id))
                {
                    yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.InvalidPackageId, Id));
                }
            }

            if (LicenseUrl == String.Empty)
            {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "LicenseUrl"));
            }

            if (IconUrl == String.Empty)
            {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "IconUrl"));
            }

            if (ProjectUrl == String.Empty)
            {
                yield return new ValidationResult(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_UriCannotBeEmpty, "ProjectUrl"));
            }

            if (RequireLicenseAcceptance && String.IsNullOrWhiteSpace(LicenseUrl))
            {
                yield return new ValidationResult(NuGetResources.Manifest_RequireLicenseAcceptanceRequiresLicenseUrl);
            }
        }

        private static IEnumerable<FrameworkName> ParseFrameworkNames(string frameworkNames)
        {
            if (String.IsNullOrEmpty(frameworkNames))
            {
                return Enumerable.Empty<FrameworkName>();
            }

            return frameworkNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(VersionUtility.ParseFrameworkName);
        }

        private static PackageDependencySet CreatePackageDependencySet(ManifestDependencySet manifestDependencySet) 
        {
            FrameworkName targetFramework = manifestDependencySet.TargetFramework == null
                                            ? null 
                                            : VersionUtility.ParseFrameworkName(manifestDependencySet.TargetFramework);

            var dependencies = from d in manifestDependencySet.Dependencies
                               select new PackageDependency(
                                   d.Id,
                                   String.IsNullOrEmpty(d.Version) ? null : VersionUtility.ParseVersionSpec(d.Version));

            var properties = manifestDependencySet.Properties == null ? Enumerable.Empty<PackageProperty>() :
                from p in manifestDependencySet.Properties
                select new PackageProperty(p.Name, p.Value);

            return new PackageDependencySet(targetFramework, dependencies, properties);
        }

        private static string GetPropertyGrouping(IEnumerable<PackageProperty> properties)
        {
            if (properties == null)
            {
                return null;
            }

            var namesValues = from propertyName in properties.Select(p => p.Name).Distinct().OrderBy(n => n)
                              let values = String.Join(",", properties.Where(p => p.Name == propertyName).Select(p => p.Value).Distinct().OrderBy(v => v))
                              select propertyName + "=" + values;

            return String.Join(";", namesValues);
        }

    }
}