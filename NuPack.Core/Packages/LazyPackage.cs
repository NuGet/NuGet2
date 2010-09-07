namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// This package wraps an existing package that may take some time to retrieve since
    /// it may come as an external resource. For example when reading metadata from the feed
    /// we never access the content, so we shouldn't need to download the package until it was
    /// necessary.
    /// </summary>
    public abstract class LazyPackage : Package {
        private Package _package;

        protected Package Package {
            get {
                if (_package == null) {
                    _package = CreatePackage();
                }
                return _package;
            }
        }

        public override IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return Package.AssemblyReferences;
            }
        }

        public override IEnumerable<PackageDependency> Dependencies {
            get {
                return Package.Dependencies;
            }
        }

        public override IEnumerable<IPackageFile> GetFiles() {
            return Package.GetFiles();
        }

        /// <summary>
        /// Get the underlying package
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be an expensive call")]
        protected abstract Package CreatePackage();
    }
}
