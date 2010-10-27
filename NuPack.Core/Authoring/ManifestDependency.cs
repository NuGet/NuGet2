using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet {
    [XmlType("dependency")]
    public class ManifestDependency {
        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_DependencyIdRequired")]
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("minVersion")]
        public string MinVersion { get; set; }

        [XmlAttribute("maxVersion")]
        public string MaxVersion { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }
    }
}