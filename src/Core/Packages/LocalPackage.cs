using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public abstract class LocalPackage : IPackage
    {
        private const string ResourceAssemblyExtension = ".resources.dll";
        private IList<IPackageAssemblyReference> _assemblyReferences;

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
                if (_assemblyReferences == null)
                {
                    var unfilteredAssemblyReferences = GetUnfilteredAssemblyReferences();
                    _assemblyReferences = FilterAssemblyReferences(unfilteredAssemblyReferences, PackageReferenceSets);
                }

                return _assemblyReferences;
            }
        }

        protected IList<PackageReferenceSet> PackageReferenceSets
        {
            get;
            private set;
        }

        public virtual IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return FrameworkAssemblies.SelectMany(f => f.SupportedFrameworks).Distinct();
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return GetFilesBase();
        }

        public abstract Stream GetStream();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This operation can be expensive.")]
        protected abstract IEnumerable<IPackageFile> GetFilesBase();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This operation can be expensive.")]
        protected abstract IEnumerable<IPackageAssemblyReference> GetUnfilteredAssemblyReferences();

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
            PackageReferenceSets = manifest.Metadata.ReferenceSets.Select(r => new PackageReferenceSet(r)).ToList();

            // Ensure tags start and end with an empty " " so we can do contains filtering reliably
            if (!String.IsNullOrEmpty(Tags))
            {
                Tags = " " + Tags + " ";
            }
        }

        internal protected static bool IsAssemblyReference(string filePath)
        {           
            // assembly reference must be under lib/
            if (!filePath.StartsWith(Constants.LibDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);

            // if it's an empty folder, yes
            if (fileName == Constants.PackageEmptyFileName)
            {
                return true;
            }

            // Assembly reference must have a .dll|.exe|.winmd extension and is not a resource assembly;
            return !filePath.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                Constants.AssemblyReferencesExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }

        private IList<IPackageAssemblyReference> FilterAssemblyReferences(
            IEnumerable<IPackageAssemblyReference> unfilteredAssemblyReferences, 
            IList<PackageReferenceSet> packageReferenceSets)
        {
            if (packageReferenceSets.IsEmpty())
            {
                return unfilteredAssemblyReferences.ToList();
            }

            var results = new List<IPackageAssemblyReference>();

            // we group assembly references by TargetFramework
            var assembliesGroupedByFx = unfilteredAssemblyReferences.ToLookup(d => d.TargetFramework);
            foreach (var group in assembliesGroupedByFx)
            {
                FrameworkName fileTargetFramework = group.Key;

                IEnumerable<PackageReferenceSet> bestMatches;
                if (VersionUtility.TryGetCompatibleItems(fileTargetFramework, PackageReferenceSets, out bestMatches))
                {
                    // now examine each assembly file, check if it appear in the References list for the correponding target framework
                    foreach (var assemblyFile in group)
                    {
                        if (bestMatches.Any(m => m.References.Contains(assemblyFile.Name)))
                        {
                            results.Add(assemblyFile);
                        }
                    }
                }
            }

            return results;
        }

        public override string ToString()
        {
            // extension method, must have 'this'.
            return this.GetFullName();
        }
    }
}