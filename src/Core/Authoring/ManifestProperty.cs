using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("property")]
    public class ManifestProperty
    {
        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_PropertyNameRequired")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_PropertyValueRequired")]
        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
