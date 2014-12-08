using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    /// <summary>
    /// Model for search results shown by NuGet.exe commandLine search.
    /// </summary>
    public class CommandLineSearchMetadata
    {
        public string Id { get; set; }
        public NuGetVersion Version { get; set; }
        public string Summary { get; set; }
        public string LicenseUrl{ get; set; }
    }
}
