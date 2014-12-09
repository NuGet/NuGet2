using NuGet.Client.Resources;
using NuGet.Client.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Repository
{
    public class VsV3MetadataResource : VsMetadataResource,IV3Resource
    {
        private NuGetV3Client _v3Client;
        private string _host;
        public VsV3MetadataResource(string sourceUrl, string host) 
        {
            _v3Client = new NuGetV3Client(sourceUrl, host);
            _host = host;
        }

        public NuGetV3Client V3Client
        {
            get
            {
                return _v3Client;
            }
            set
            {
                _v3Client = value;
            }
        }

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value;
            }
        }
    
        public override VisualStudioUIPackageMetadata GetPackageMetadataForVisualStudioUI(string packageId, Versioning.NuGetVersion version)
        {
 	        throw new NotImplementedException();
        }
}
}
