using NuGet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Represents a resource provided by a V2 server. [ Like search resource, metadata resource]
    /// *TODOS: Add a trace source , Resource description ?
    /// </summary>
    public class V2Resource :Resource
    {
        private IPackageRepository _v2Client;
        public  string _host;
        private string _description = "A Resource exposed by V2 server endpoint.";
                     
        public V2Resource(IPackageRepository repo,string host):base(host)
        {
            _v2Client = repo;
            _host = host;           
        }

        public V2Resource(V2Resource v2Resource)
            : base(v2Resource.Host)
        {
            _v2Client = v2Resource.V2Client;
            _host = v2Resource.Host;
        }
       
         public IPackageRepository V2Client
        {
            get
            {
                return _v2Client;
            }           
        }

         public override string Description
         {
             get { return _description; }
         }
    }
}
