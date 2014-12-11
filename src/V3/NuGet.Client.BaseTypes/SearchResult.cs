using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public class SearchResult
    {
        public string Id;
        public NuGetVersion Version;
        public string Description;
        public string Summary;
        public Uri IconUrl { get; set; }
        public IEnumerable<NuGetVersion> Versions { get; set; }
       // public PackageMetadata latestPackageMetadata { get; set; }
    }
}
