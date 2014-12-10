using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;
using NuGet.Versioning;

namespace NuGet.Client.V2
{
    public class V2DownloadResource : V2Resource,IDownloadResource
    {
        private string _description = "Resource that helps in downloading a package from the V2 server endpoint.";
        public V2DownloadResource(IPackageRepository repo,string host):base(repo,host)
        {

        }

        public override string Description
        {
            get { return _description;}
        }

        public Uri GetNupkgUrlForDownload(string id, NuGetVersion version)
        {
            throw new NotImplementedException();
        }
    }
}
