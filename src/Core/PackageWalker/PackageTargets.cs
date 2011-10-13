using System;

namespace NuGet
{
    [Flags]
    public enum PackageTargets
    {
        None = 0,
        Project = 1,
        External = 2,
        All = Project | External
    }
}
