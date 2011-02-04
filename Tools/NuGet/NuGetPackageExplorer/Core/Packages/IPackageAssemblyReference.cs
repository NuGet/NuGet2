namespace NuGet {
    using System;
    using System.Runtime.Versioning;

    public interface IPackageAssemblyReference : IPackageFile {
        FrameworkName TargetFramework {
            get;
        }

        string Name {
            get;
        }
    }
}
