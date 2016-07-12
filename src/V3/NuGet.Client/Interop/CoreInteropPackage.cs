using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly OldSemVer _oldVer;
        
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
            _oldVer = CoreConverters.SafeToSemanticVersion(Version);
        
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
                NuGetTraceSources.CoreInterop.Verbose("packagetarget", "Returning dummy package target value. Need to fill this in with the right data!");
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
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public bool IsLatestVersion
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public DateTimeOffset? Published
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public IEnumerable<System.Runtime.Versioning.FrameworkName> GetSupportedFrameworks()
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public System.IO.Stream GetStream()
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public string Title
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IEnumerable<string> Authors
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IEnumerable<string> Owners
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public Uri IconUrl
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public Uri LicenseUrl
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public Uri ProjectUrl
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public bool RequireLicenseAcceptance
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public bool DevelopmentDependency
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public string Description
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public string Summary
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public string ReleaseNotes
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public string Tags
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public string Copyright
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
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
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IEnumerable<ManifestContentFiles> ContentFiles
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException();
            }
        }

        public Uri ReportAbuseUrl
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public int DownloadCount
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }
        #endregion
    }
}
