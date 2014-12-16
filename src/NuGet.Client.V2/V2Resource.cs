using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Represents a resource provided by a V2 server. [ Like search resource, metadata resource]
    /// *TODOS: Add a trace source , Resource description ?
    /// </summary>
    public abstract class V2Resource :Resource
    {
        private IPackageRepository _v2Client;
        private string _host;
        private string _description;
                     
        public V2Resource(IPackageRepository repo,string host):base(host)
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
