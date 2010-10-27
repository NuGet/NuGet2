namespace NuGet {
    using System;
    using System.Diagnostics;
    using System.IO.Packaging;
    using System.Runtime.Versioning;
    using System.Text;

    internal class ZipPackageAssemblyReference : ZipPackageFile, IPackageAssemblyReference {
        private FrameworkName _targetFramework;

        public ZipPackageAssemblyReference(PackagePart part)
            : base(part) {
            // The path for a reference might look like this for assembly foo.dll:
            // lib\{FrameworkName}{Version}\foo.dll

            Debug.Assert(Path.StartsWith("lib", StringComparison.OrdinalIgnoreCase), "path doesn't start with lib");

            // Get rid of the lib folder            
            string path = Path.Substring(3).Trim(System.IO.Path.DirectorySeparatorChar);

            // Get the target framework string if specified
            string targetFrameworkString = System.IO.Path.GetDirectoryName(path).Trim(System.IO.Path.DirectorySeparatorChar);
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

        public override string ToString() {
            var builder = new StringBuilder();
            if (TargetFramework != null) {
                builder.Append(TargetFramework).Append(" ");
            }
            builder.Append(Name).AppendFormat(" ({0})", Path);
            return builder.ToString();
        }
    }
}
