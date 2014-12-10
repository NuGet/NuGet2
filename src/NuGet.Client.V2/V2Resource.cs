using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.BaseTypes;

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
                     
        public V2Resource(IPackageRepository repo,string host,string description):base(host,description)
        {
            _v2Client = repo;
            _host = host;
            _description = description;
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
