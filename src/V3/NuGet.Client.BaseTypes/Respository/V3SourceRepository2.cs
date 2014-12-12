using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using NuGet.Client.V3;
using System.Runtime.Versioning;
using NuGet.Client.Resources;

namespace NuGet.Client
{
    /// <summary>
    /// Respository which exposes various resources like Search resource, metrics resource for the APi v3 endpoint.
    /// Uses the NuGetV3Client to talk to the endpoint.
    /// *TODOS: Remove the direct methods like Search, GetPackageMetadata ....
    /// *TODOs: Version Utility.
    /// </summary>
    public class V3SourceRepository2 : SourceRepository
    {       
        private PackageSource _source;      
        private NuGetV3Client _client;
       
        public override PackageSource Source
        {
            get { return _source; }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The HttpClient can be left open until VS shuts down.")]
        public V3SourceRepository2(PackageSource source, string host)
        {
            _source = source;          
            _client = new NuGetV3Client(source.Url, host);
           // AddResource<SearchResource>(() => new V3SearchResource(source.Url,host));         
        }
      
        public async override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            List<string> frameworkNames = new List<string>();
            foreach (FrameworkName fx in filters.SupportedFrameworks)
                // frameworkNames.Add(VersionUtility.GetShortFrameworkName(fx));
                frameworkNames.Add(fx.FullName);
            return await _client.Search(searchTerm, frameworkNames, filters.IncludePrerelease, skip, take, cancellationToken);
        }

        // Async void because we don't want metric recording to block anything at all
        public override void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, IInstallationTarget target)
        {
           //No op
        }

        // +++ this would not be needed once the result matches searchResult.
     
        public override async Task<JObject> GetPackageMetadata(string id, NuGetVersion version)
        {
            return await _client.GetPackageMetadata(id, version);
        }

        public override async Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
        {
            return await _client.GetPackageMetadataById(packageId);
         
        }

    }
}
