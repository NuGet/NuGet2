using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NuGet
{
    [XmlType("packageType")]
    [Serializable]
    public class PackageTypeMetadata
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
