using System.Runtime.Versioning;

namespace NuPack {
    public class PhysicalAssemblyReference : PhysicalPackageFile, IPackageAssemblyReference  {
        public FrameworkName TargetFramework {
            get; 
            set;
        }
    }
}
