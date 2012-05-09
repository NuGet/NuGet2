using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet
{
    public abstract class LocalPackage : IPackage
    {
        private const string ResourceAssemblyExtension = ".resources.dll";
        private HashSet<string> _references;

        protected LocalPackage()
        {
        }

        public string Id
        {
            get;
            set;
        }

        public SemanticVersion Version
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public IEnumerable<string> Authors
        {
            get;
            set;
        }

        public IEnumerable<string> Owners
        {
            get;
            set;
        }

        public Uri IconUrl
        {
            get;
            set;
        }

        public Uri LicenseUrl
        {
            get;
            set;
        }

        public Uri ProjectUrl
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get
            {
                return null;
            }
        }

        public int DownloadCount
        {
            get
            {
                return -1;
            }
        }

        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get
            {
                return true;
            }
        }

        public bool IsLatestVersion
        {
            get
            {
                return this.IsReleaseVersion();
            }
        }

        public bool Listed
        {
            get
            {
                return true;
            }
        }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get;
            set;
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get
            {
                return GetAssemblyReferencesBase();
            }
        }

        protected IList<ManifestReference> ManifestReferences
        {
            get;
            private set;
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return GetFilesBase();
        }
        
        public abstract Stream GetStream();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This operation can be expensive.")]
        protected abstract IEnumerable<IPackageFile> GetFilesBase();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="This operation can be expensive.")]
        protected abstract IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesBase();

        protected void ReadManifest(Stream manifestStream)
        {
            Manifest manifest = Manifest.ReadFrom(manifestStream);
            IPackageMetadata metadata = manifest.Metadata;

            Id = metadata.Id;
            Version = metadata.Version;
            Title = metadata.Title;
            Authors = metadata.Authors;
            Owners = metadata.Owners;
            IconUrl = metadata.IconUrl;
            LicenseUrl = metadata.LicenseUrl;
            ProjectUrl = metadata.ProjectUrl;
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
            Description = metadata.Description;
            Summary = metadata.Summary;
            ReleaseNotes = metadata.ReleaseNotes;
            Language = metadata.Language;
            Tags = metadata.Tags;
            DependencySets = metadata.DependencySets;
            FrameworkAssemblies = metadata.FrameworkAssemblies;
            Copyright = metadata.Copyright;
            ManifestReferences = manifest.Metadata.References;

            IEnumerable<string> references = (ManifestReferences ?? Enumerable.Empty<ManifestReference>()).Select(c => c.File);
            _references = new HashSet<string>(references, StringComparer.OrdinalIgnoreCase);

            // Ensure tags start and end with an empty " " so we can do contains filtering reliably
            if (!String.IsNullOrEmpty(Tags))
            {
                Tags = " " + Tags + " ";
            }
        }

        protected bool IsAssemblyReference(IPackageFile file)
        {
            if (_references == null)
            {
                throw new InvalidOperationException(NuGetResources.Manifest_NotAvailable);
            }

            return IsAssemblyReference(file, _references);
        }

        protected bool IsAssemblyReference(string filePath)
        {
            if (_references == null)
            {
                throw new InvalidOperationException(NuGetResources.Manifest_NotAvailable);
            }

            return IsAssemblyReference(filePath, _references);
        }

        internal static bool IsAssemblyReference(IPackageFile file, IEnumerable<string> references)
        {
            return IsAssemblyReference(file.Path, references);
        }

        internal static bool IsAssemblyReference(string filePath, IEnumerable<string> references)
        {
            // Assembly references are in lib/ and have a .dll/.exe/.winmd extension OR if it is an empty folder.
            var fileName = Path.GetFileName(filePath);

            return filePath.StartsWith(Constants.LibDirectory, StringComparison.OrdinalIgnoreCase) &&
                    // empty file
                   (fileName == Constants.PackageEmptyFileName || 
                    // Exclude resource assemblies
                    !filePath.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                    Constants.AssemblyReferencesExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase) &&
                    // If references are listed, ensure that the file is listed in it.
                    (references.IsEmpty() || references.Contains(fileName)));
        }

        public override string ToString()
        {
            return this.GetFullName();
        }
    }
}