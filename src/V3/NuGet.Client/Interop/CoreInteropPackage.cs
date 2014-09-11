using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using OldSemVer = NuGet.SemanticVersion;

namespace NuGet.Client.Interop
{
    internal class CoreInteropPackage : IPackage
    {
        private OldSemVer _oldVer;
        private JObject _json;

        public string Id { get; private set; }
        public NuGetVersion Version { get; private set; }

        OldSemVer IPackageName.Version
        {
            get { return _oldVer; }
        }
 
        public CoreInteropPackage(string id, NuGetVersion version)
        {
            Id = id;
            Version = version;
            _oldVer = new SemanticVersion(version.Version, version.Release);
        }

        public CoreInteropPackage(JObject j)
            : this(j.Value<string>("id"), NuGetVersion.Parse(j.Value<string>("version")))
        {
            _json = j;
        }

        #region Unimplemented Parts
        public bool IsAbsoluteLatestVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsLatestVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool Listed
        {
            get { throw new NotImplementedException(); }
        }

        public DateTimeOffset? Published
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream GetStream()
        {
            throw new NotImplementedException();
        }

        public string Title
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> Authors
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> Owners
        {
            get { throw new NotImplementedException(); }
        }

        public Uri IconUrl
        {
            get { throw new NotImplementedException(); }
        }

        public Uri LicenseUrl
        {
            get { throw new NotImplementedException(); }
        }

        public Uri ProjectUrl
        {
            get { throw new NotImplementedException(); }
        }

        public bool RequireLicenseAcceptance
        {
            get { throw new NotImplementedException(); }
        }

        public bool DevelopmentDependency
        {
            get { throw new NotImplementedException(); }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        public string Summary
        {
            get { throw new NotImplementedException(); }
        }

        public string ReleaseNotes
        {
            get { throw new NotImplementedException(); }
        }

        public string Language
        {
            get { throw new NotImplementedException(); }
        }

        public string Tags
        {
            get { throw new NotImplementedException(); }
        }

        public string Copyright
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get { throw new NotImplementedException(); }
        }

        public Version MinClientVersion
        {
            get { throw new NotImplementedException(); }
        }

        public Uri ReportAbuseUrl
        {
            get { throw new NotImplementedException(); }
        }

        public int DownloadCount
        {
            get { throw new NotImplementedException(); }
        }
        #endregion
    }
}
