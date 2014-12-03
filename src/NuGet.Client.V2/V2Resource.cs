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
        protected IPackageRepository _v2Client;              
      
        public V2Resource(V2Resource v2Resource)            
        {
            _v2Client = v2Resource.V2Client;
            _host = v2Resource.Host;
        }
        public V2Resource(IPackageRepository repo,string host)
        {
            _v2Client = repo;
            _host = host;
        }

        public IPackageRepository V2Client
        {
            get
            {
                return _v2Client;
            }
        }       
    }
}
