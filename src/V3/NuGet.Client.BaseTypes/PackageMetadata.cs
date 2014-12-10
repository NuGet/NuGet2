using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.BaseTypes
{
    public class PackageMetadata
    {
        public string Id;
        public NuGetVersion Version;
        public string Description;
        public string Summary;
    }
}
