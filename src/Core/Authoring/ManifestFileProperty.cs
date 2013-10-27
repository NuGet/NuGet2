using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("property")]
    public class ManifestFileProperty
    {
        [Required(ErrorMessageResourceType = typeof (NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof (NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}