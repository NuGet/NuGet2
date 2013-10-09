using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace NuGet.WebMatrix.DependentTests
{
    internal class IPackageMock : IPackage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IPackageMock"/> class.
        /// </summary>
        public IPackageMock(string id)
        {
            Id = id;
            Tags = string.Empty;
            this.IsLatestVersion = true;
        }

        #region IPackage Members

        public object Tag
        {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get;
            set;
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Enumerable.Empty<IPackageFile>();
        }

        public System.IO.Stream GetStream()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> Authors
        {
            get;
            set;
        }

        public IEnumerable<PackageDependency> Dependencies
        {
            get;
            set;
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

        public Version Version
        {
            get;
            set;
        }

        public int DownloadCount
        {
            get;
            set;
        }

        public double Rating
        {
            get;
            set;
        }

        public int RatingsCount
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get;
            set;
        }

        public bool IsLatestVersion
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get { throw new NotImplementedException(); }
        }

        public string Copyright
        {
            get { throw new NotImplementedException(); }
        }

        public DateTimeOffset? Published
        {
            get { throw new NotImplementedException(); }
        }


        public bool IsAbsoluteLatestVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool Listed
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IPackageMetadata Members

        SemanticVersion IPackageMetadata.Version
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get { return Enumerable.Empty<PackageDependencySet>(); }
        }

        public Version MinClientVersion
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return (ICollection<PackageReferenceSet>)Enumerable.Empty<PackageReferenceSet>(); }
        }

        #endregion

    }
}
