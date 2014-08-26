using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;

namespace NuGet.VisualStudio.ClientV3
{
    public class SearchFilter
    {
        public IEnumerable<FrameworkName> SupportedFrameworks { get; set; }
        public bool IncludePrerelease { get; set; }
    }
}
