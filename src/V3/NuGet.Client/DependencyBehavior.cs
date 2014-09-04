using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client
{
    public enum DependencyBehavior
    {
        Ignore,
        Lowest,
        HighestPatch,
        HighestMinor,
        Highest
    }
}
