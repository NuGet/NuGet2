using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.V2;
using System.ComponentModel.Composition;
using System;

namespace NuGet.Client
{
    /// <summary>
    /// V2SourceRepository which exposes various resources like SearchResource, MetricsResource and so on...
    /// *TODOS: Add tracing
    /// </summary>
    [Export(typeof(SourceRepository))]
    public class V2SourceRepository2 : SourceRepository
    {
        private readonly IPackageRepository _repository;
        private readonly LocalPackageRepository _lprepo;
        private readonly PackageSource _source;
        private readonly string _userAgent;
        [ImportMany(typeof(V2Resource))]
        private IEnumerable<Resource> _resources;


        public override IEnumerable<Resource> Resources
        {
            get
            {
                return _resources;
            }
        }
        public override PackageSource Source { get { return _source; } }

        public V2SourceRepository2()
        {

        }
        public V2SourceRepository2(PackageSource source, string host)
        {
            _source = source;
            _repository = new PackageRepositoryFactory().CreateRepository(source.Url);

            _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client.Interop", host);

            var events = _repository as IHttpClientEvents;
            if (events != null)
            {
                events.SendingRequest += (sender, args) =>
                {
                    var httpReq = args.Request as HttpWebRequest;
                    if (httpReq != null)
                    {
                        httpReq.UserAgent = _userAgent;
                    }                    
                };
            }

            _lprepo = _repository as LocalPackageRepository;
            
        }



        public override Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<Newtonsoft.Json.Linq.JObject> GetPackageMetadata(string id, Versioning.NuGetVersion version)
        {
            throw new System.NotImplementedException();
        }

        public override Task<IEnumerable<Newtonsoft.Json.Linq.JObject>> GetPackageMetadataById(string packageId)
        {
            throw new System.NotImplementedException();
        }

        public override void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, IInstallationTarget target)
        {
            throw new System.NotImplementedException();
        }

        public override bool TryGetRepository(PackageSource source)
        {
            return IsV2(source);
        }

        public override SourceRepository GetRepository(PackageSource source)
        {
            return new V2SourceRepository2(source, "testapp");
        }

        private static bool IsV2(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return true;
            }

            using (var client = new Data.DataClient())
            {
                var result = client.GetFile(url);
                if (result == null)
                {
                    return false;
                }

                var raw = result.Result.Value<string>("raw");
                if (raw != null && raw.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }

                return false;
            }
        }
    }
}