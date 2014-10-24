using NuGet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Resolution
{
    public class ResolutionContext
    {
        public DependencyBehavior DependencyBehavior { get; set; }
        public bool AllowPrerelease { get; set; }
    }
}
