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
    public abstract class LazyPackage : IPackage {
        private IPackage _package;
        public abstract IEnumerable<string> Authors {
            get;
        }

        public abstract string Category {
            get;
        }

        public abstract DateTime Created {
            get;
        }

        public abstract string Description {
            get;
        }

        public abstract string Id {
            get;
        }

        public abstract IEnumerable<string> Keywords {
            get;
        }

        public abstract string Language {
            get;
        }

        public abstract string LastModifiedBy {
            get;
        }

        public abstract bool RequireLicenseAcceptance {
            get;
        }

        public abstract Uri LicenseUrl {
            get;
        }

        public abstract DateTime Modified {
            get;
        }

        public abstract Version Version {
            get;
        }

        protected IPackage Package {
            get {
                if (_package == null) {
                    _package = CreatePackage();
                }
                return _package;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return Package.AssemblyReferences;
            }
        }

        public virtual IEnumerable<PackageDependency> Dependencies {
            get {
                return Package.Dependencies;
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            return Package.GetFiles();
        }

        /// <summary>
        /// Get the underlying package
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be an expensive call")]
        protected abstract IPackage CreatePackage();        
    }
}
