using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {

    /// <summary>
    /// Represents a Recent package.
    /// </summary>
    public class RecentPackage : IPackage, IPersistencePackageMetadata {

        private IPackage _basePackage;
        private readonly string _source;
        //private readonly IPersistencePackageMetadata _packageMetadata;
        //private readonly IPackageRepositoryFactory _repositoryFactory;

        /// <summary>
        /// This constructor is used during serialization.
        /// </summary>
        public RecentPackage(IPackage basePackage, string source) {
            if (basePackage == null) {
                throw new ArgumentNullException("basePackage");
            }

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            _source = source;
            _basePackage = basePackage;
        }

        /// <summary>
        /// This constructor is used during deserialization
        /// </summary>
        /// <param name="packageMetadata"></param>
        /// <param name="repositoryFactory"></param>
        public RecentPackage(IPersistencePackageMetadata packageMetadata, IPackageRepositoryFactory repositoryFactory) {
            if (packageMetadata == null) {
                throw new ArgumentNullException("packageMetadata"); 
            }

            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            _source = packageMetadata.Source;
            
            _basePackage = FindPackageFromRepository(packageMetadata, repositoryFactory);
            if (_basePackage == null) {
                // can't retrieve the package from the source. throw.
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        VsResources.UnableToFindPackageFromSource,
                        packageMetadata.Id,
                        packageMetadata.Version,
                        packageMetadata.Source), 
                        "packageMetadata");
            }
        }

        private static IPackage FindPackageFromRepository(IPersistencePackageMetadata packageMetadata, IPackageRepositoryFactory repositoryFactory) {
            Debug.Assert(repositoryFactory != null);
            Debug.Assert(packageMetadata != null);

            var packageSource = new PackageSource(packageMetadata.Source);
            // be careful with the aggreate source. in this case, the source is a pseudo source, '(Aggregate Source)'
            if (packageMetadata.Source.Equals(VsPackageSourceProvider.AggregateSource.Source, StringComparison.OrdinalIgnoreCase)) {
                packageSource.IsAggregate = true;
            }

            var repository = repositoryFactory.CreateRepository(packageSource);
            return repository == null ? null : repository.FindPackage(packageMetadata.Id, packageMetadata.Version);
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return _basePackage.AssemblyReferences; 
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            return _basePackage.GetFiles();
        }

        public Stream GetStream() {
            return _basePackage.GetStream();
        }

        public string Id {
            get {
                return _basePackage.Id;
            }
        }

        public Version Version {
            get {
                return _basePackage.Version;
            }
        }

        public string Source {
            get {
                return _source;
            }
        }

        public string Title {
            get {
                return _basePackage.Title;
            }
        }

        public IEnumerable<string> Authors {
            get {
                return _basePackage.Authors;
            }
        }

        public IEnumerable<string> Owners {
            get {
                return _basePackage.Owners;
            }
        }

        public Uri IconUrl {
            get {
                return _basePackage.IconUrl;
            }
        }

        public Uri LicenseUrl {
            get {
                return _basePackage.LicenseUrl;
            }
        }

        public Uri ProjectUrl {
            get {
                return _basePackage.ProjectUrl;
            }
        }

        public bool RequireLicenseAcceptance {
            get {
                return _basePackage.RequireLicenseAcceptance;
            }
        }

        public string Description {
            get {
                return _basePackage.Description;
            }
        }

        public string Summary {
            get {
                return _basePackage.Summary;
            }
        }

        public string Language {
            get {
                return _basePackage.Language;
            }
        }

        public string Tags {
            get {
                return _basePackage.Tags;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return _basePackage.Dependencies;
            }
        }

        public Uri ReportAbuseUrl {
            get {
                return _basePackage.ReportAbuseUrl;
            }
        }

        public int DownloadCount {
            get {
                return _basePackage.DownloadCount;
            }
        }

        public int RatingsCount {
            get {
                return _basePackage.RatingsCount;
            }
        }

        public double Rating {
            get {
                return _basePackage.Rating;
            }
        }
    }
}