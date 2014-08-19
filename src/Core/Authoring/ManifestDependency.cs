using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("dependency")]
    public class ManifestDependency
    {
        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_DependencyIdRequired")]
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is required by XML serializer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "This is required by XML serializer.")]
        [XmlElement("propertyConstraints")]
        public List<ManifestProperty> PropertyConstraints { get; set; }
    }
}