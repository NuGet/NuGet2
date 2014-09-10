using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Resolution
{
    public class ResolutionContext
    {
        public DependencyBehavior DependencyBehavior { get; set; }
        public bool AllowPrerelease { get; set; }
    }
}
