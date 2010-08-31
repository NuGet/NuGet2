namespace NuPack {
    using System.IO.Packaging;
    using System;
    using System.Runtime.Versioning;

    internal class PackageAssemblyReference : PackageFile, IPackageAssemblyReference {
        private FrameworkName _targetFramework;

        public PackageAssemblyReference(PackagePart part)
            : base(part) {
            // The path for a reference might look like this for assembly foo.dll:
            // {FrameworkName}{Version}/foo.dll            

            // Get the target framework string if specified
            string targetFrameworkString = System.IO.Path.GetDirectoryName(Path).Trim('/');
            if (!String.IsNullOrEmpty(targetFrameworkString)) {
                _targetFramework = Utility.ParseFrameworkName(targetFrameworkString);
            }
        }

        public FrameworkName TargetFramework {
            get {
                return _targetFramework;
            }
        }

        public string Name {
            get {
                return System.IO.Path.GetFileName(Path);
            }
        }
    }
}
