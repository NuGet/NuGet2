using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.Client
{
    public class SearchFilter
    {
        public IEnumerable<string> SupportedFrameworks { get; set; }
        public bool IncludePrerelease { get; set; }
    }
}
