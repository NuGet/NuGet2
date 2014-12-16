using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    /// <summary>
    /// Represents a resource provided by a server endpoint (V2 or V3).
    /// *TODOs: Add a trace ?
    /// </summary>
    public abstract class Resource
    {
        private string _host;
        private string _description;

        public Resource()
        {

        }
                     
        public Resource(string host)
        {         
            _host = host;            
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
            get;            
        } 
            
    }
}
