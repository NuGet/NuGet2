using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
  /// <summary>
    /// Represents a Server endpoint. Exposes methods to get a specific resource like Search resoure, Metrics service and so on for the given server endpoint.
    /// </summary>
    // TODO: it needs to implement IDisposable.
    // *TODOs: Define RequiredResourceNotFound exception instead of general exception.       
    public  class SourceRepository2
    {
        [ImportMany]
        private IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> _providers { get; set; }
        private readonly PackageSource _source;
        private static IDictionary<string, object> _cache = new Dictionary<string, object>();

        public SourceRepository2(PackageSource source, IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> providers)
        {
            _source = source;
            _providers = providers;
        }

        public object GetResource(Type resourceType)
        {            
            foreach(Lazy<IResourceProvider,IResourceProviderMetadata>  provider in _providers)
            {
                if (provider.Metadata.ResourceType == resourceType)
                {
                    Resource resource = null;
                    if (provider.Value.TryCreateResource(_source, ref _cache, out resource))
                    {
                        return resource;
                    }
                }
            }
            return null;
        }
       
        public T GetResource<T>() { return (T)GetResource(typeof(T)); }
    }
}
