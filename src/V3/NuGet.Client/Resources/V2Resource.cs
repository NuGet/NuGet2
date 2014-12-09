using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    /// <summary>
    /// Represents a resource provided by a V2 server. [ Like search resource, metadata resource]
    /// *TODOS: Add a trace source , Resource description ?
    /// </summary>
    public interface V2Resource
    {
        private IPackageRepository _v2Client;
        private string _host;
        private string _description;
                     
        public V2Resource(IPackageRepository repo,string host,string description)
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

        public string Host
        {
            get
            {
                return _host;
            }
           
        }
        public abstract string Description
        {
            get
            {
                return _description;
            }
        }
        

       
    }
}
