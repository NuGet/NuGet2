using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client
{
    public class ResolverContext
    {
        public DependencyBehavior DependencyBehavior { get; set; }
        public bool AllowPrerelease { get; set; }
    }
}
