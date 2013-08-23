using System;

namespace NuGet
{
    public static class EnvironmentUtility
    {
        private static readonly bool _isMonoRuntime = Type.GetType("Mono.Runtime") != null;

        public static bool IsMonoRuntime
        {
            get
            {
                return _isMonoRuntime;
            }
        }
    }
}
