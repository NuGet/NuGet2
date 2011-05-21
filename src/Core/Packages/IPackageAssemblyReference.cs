namespace NuGet {
    using System.Runtime.Versioning;

    public interface IPackageAssemblyReference : IPackageFile, IFrameworkTargetable {
        FrameworkName TargetFramework {
            get;
        }

        string Name {
            get;
        }
    }
}
