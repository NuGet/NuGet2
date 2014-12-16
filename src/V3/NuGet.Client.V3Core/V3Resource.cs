using NuGet.Client.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3
{
    /// <summary>
    /// Represents a resource provided by a V3 server. [ Like search resource, metadata resource]
    /// </summary>
    public class V3Resource : Resource
    {
        private NuGetV3Client _v3Client;
        private string _description = "Resource provided by a V3 server endpoint."

        public V3Resource(string sourceUrl, string host)
            : base(host)
        {
            _v3Client = new NuGetV3Client(sourceUrl, host);

        }

        public V3Resource(NuGetV3Client client)           
        {
            _v3Client = client;

        }

        public NuGetV3Client V3Client
        {
            get
            {
                return _v3Client;
            }
        }

        public override string Description
        {
            get { return _description; }
        }
    }
}
