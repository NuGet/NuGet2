using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.V3Interop;
using NuGet.Versioning;
using OldSemVer = NuGet.SemanticVersion;

namespace NuGet.Client.Interop
{
    internal class CoreInteropPackage : IPackage, IV3PackageMetadata
    {
        private OldSemVer _oldVer;
        
        public string Id { get; private set; }
        public NuGetVersion Version { get; private set; }
        public JObject Json { get; private set; }

        OldSemVer IPackageName.Version
        {
            get { return _oldVer; }
        }
 
        public CoreInteropPackage(JObject json)
        {
            Id = json.Value<string>("id");
            Version = NuGetVersion.Parse(json.Value<string>("version"));
            _oldVer = CoreConverters.SafeToSemVer(Version);
        
            Json = json;
        }

        public Version MinClientVersion
        {
            get { return TryGet<Version>(Properties.MinimumClientVersion, System.Version.Parse, expected: true); }
        }

        public string Language
        {
            get { return TryGet(Properties.Language, expected: true); }
        }
        
        public PackageTargets PackageTarget
        {
            get
            {
                NuGetTraceSources.CoreInterop.Error("packagetarget", "Returning dummy package target value. Need to fill this in with the right data!");
                return PackageTargets.Project;
            }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get 
            {
                var sets = Json.Value<JArray>(Properties.DependencyGroups);
                if (sets != null)
                {
                    return sets.Select(t => PackageJsonLd.DependencySetFromJson((JObject)t));
                }
                else
                {
                    return Enumerable.Empty<PackageDependencySet>();
                }
            }
        }

        // Listed doesn't matter!
        public bool Listed
        {
            get { return true; }
        }

        private T TryGet<T>(string name, Func<string, T> parser, bool expected = false)
        {
            return TryGet(name, default(T), parser, expected);
        }

        private T TryGet<T>(string name, T defaultValue, Func<string, T> parser, bool expected = false)
        {
            string val = TryGet(name, expected);

            if (String.IsNullOrEmpty(val))
            {
                return defaultValue;
            }
            return parser(val);
        }

        private string TryGet(string name, bool expected = false)
        {
            string val = Json.Value<string>(name);
            if (expected && val == null)
            {
                NuGetTraceSources.CoreInterop.Warning("missingexpectedjsonprop", "Expected {0} property to be surfaced in JSON-LD but it wasn't", name);
            }
            return val;
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
            get {
                NuGetTraceSources.CoreInterop.Error("faking_fxasms", "Returning empty framework assemblies because we haven't figured that out yet :)");
                return Enumerable.Empty<FrameworkAssemblyReference>();
            }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
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
