using System;

namespace NuPack {
    [Flags]
    public enum PackageTargets {
        None = 0,
        Project = 1,
        External = 2,
        Both = Project | External
    }
}
