using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public static class FrameworkNameHelpers
    {
        public static FrameworkName ParseFrameworkName(string frameworkName)
        {
            return VersionUtility.ParseFrameworkName(frameworkName);
        }
    }
}
