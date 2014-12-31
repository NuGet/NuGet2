using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public static class FrameworkNameHelper
    {
        public static FrameworkName ParsePossiblyShortenedFrameworkName(string name)
        {
            if (name.Contains(","))
            {
                return new FrameworkName(name);
            }
            return VersionUtility.ParseFrameworkName(name);
        }

        public static string GetShortFrameworkName(FrameworkName frameworkName)
        {
            return VersionUtility.GetShortFrameworkName(frameworkName);
        }
    }
}
