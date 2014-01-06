using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using NuGet;

namespace NuGet.WebMatrix.Tests.Utilities
{
    public class PackageStub : IPackage
    {
        public PackageStub(string id)
            : this(id, new Version(1, 0))
        {
        }

        public PackageStub(string id, Version version)
            : this(id, version, null)
        {
        }

        public PackageStub(string id, Version version, IEnumerable<PackageDependency> dependencies)
            : this(id, version, dependencies, null)
        {

        }

        public PackageStub(string id, Version version, IEnumerable<PackageDependency> dependencies, IEnumerable<FrameworkName> supportedFrameworks)
        {
            this.Id = id;
            this.Version = new SemanticVersion(version);
            this.IsLatestVersion = true;
            this.Dependencies = dependencies;
            this.SupportedFrameworks = supportedFrameworks ?? Enumerable.Empty<FrameworkName>();
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Enumerable.Empty<IPackageFile>();
        }

        public System.IO.Stream GetStream()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return SupportedFrameworks;
        }

        private IEnumerable<FrameworkName> SupportedFrameworks
        {
            get;
            set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get;
            set;
        }

        public bool IsLatestVersion
        {
            get;
            set;
        }

        public bool Listed
        {
            get;
            set;
        }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public IEnumerable<string> Authors
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public IEnumerable<PackageDependency> Dependencies
        {
            get;
            set;
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get { return Enumerable.Empty<PackageDependencySet>(); }
        }

        public string Description
        {
            get;
            set;
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get;
            set;
        }

        public Uri IconUrl
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public Uri LicenseUrl
        {
            get;
            set;
        }

        public IEnumerable<string> Owners
        {
            get;
            set;
        }

        public Uri ProjectUrl
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public SemanticVersion Version
        {
            get;
            set;
        }

        public int DownloadCount
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get;
            set;
        }

        public Version MinClientVersion
        {
            get;
            set;
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return (ICollection<PackageReferenceSet>)Enumerable.Empty<PackageReferenceSet>(); }
        }

        public override bool Equals(object obj)
        {
            PackageStub packageStub = obj as PackageStub;
            if (packageStub == null)
            {
                return false;
            }

            return (packageStub.Id == this.Id && packageStub.Version == this.Version);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^ this.Version.GetHashCode();
        }
    }
}
