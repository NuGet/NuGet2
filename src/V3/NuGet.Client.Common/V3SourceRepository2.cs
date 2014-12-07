using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Data;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonLD.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NuGet.Client.Resolution;
using System.Net.Http;
using System.Globalization;
using NuGet.Client.Installation;
using System.Threading;
using NuGet.Client.V3;
using System.Runtime.Versioning;

namespace NuGet.Client.Common
{
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
         
        }
      
        public async override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            List<string> frameworkNames = new List<string>();
            foreach(FrameworkName fx in filters.SupportedFrameworks)
                frameworkNames.Add(VersionUtility.GetShortFrameworkName(fx));
            return await _client.Search(searchTerm, frameworkNames, filters.IncludePrerelease, skip, take, cancellationToken);
        }

        // Async void because we don't want metric recording to block anything at all
        public override void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, InstallationTarget target)
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
